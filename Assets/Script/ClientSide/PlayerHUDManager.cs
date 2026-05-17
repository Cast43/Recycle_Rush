using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

public class PlayerHUDManager : MonoBehaviour
{
    public static PlayerHUDManager Instance;

    private World _clientWorld;
    private World _ServerWorld;

    private EntityQuery _localPlayerQuery;
    private EntityQuery _waveQuery;
    private EntityQuery _networkTimeQuery;
    private EntityQuery _tickRateQuery;
    private EntityQuery _garbageInventoryQuery;

    [SerializeField] private UnityEngine.UI.Slider energyPercentageSlider;
    [SerializeField] private UnityEngine.UI.Slider robotHealthPercentageSlider;
    [SerializeField] private TextMeshProUGUI waveCountText;
    [SerializeField] private UnityEngine.UI.Slider experienceBar;
    [SerializeField] private UnityEngine.UI.Slider energyCooldownSlider;
    [SerializeField] private UnityEngine.UI.Slider healthCooldownSlider;
    [SerializeField] private GameObject LoseHUD;

    [Header("Garbage UI")]
    [SerializeField] private TextMeshProUGUI plasticText;
    [SerializeField] private TextMeshProUGUI paperText;
    [SerializeField] private TextMeshProUGUI glassText;
    [SerializeField] private TextMeshProUGUI metalText;
    [SerializeField] private TextMeshProUGUI organicText;
    [SerializeField] private TextMeshProUGUI totalGarbageText;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        if (_clientWorld == null || !_clientWorld.IsCreated)
        {
            FindClientWorld();
            return;
        }
        if (_ServerWorld == null || !_ServerWorld.IsCreated)
        {
            FindServertWorld();
            return;
        }

        UpdateLocalPlayerData();
        UpdateGlobalData();
        UpdateVisualUpgradesVisibility();
    }

    private void FindClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsClient() && !world.IsThinClient())
            {
                _clientWorld = world;
                var em = _clientWorld.EntityManager;

                // Query do Player
                _localPlayerQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<GhostOwnerIsLocal>(),
                    ComponentType.ReadOnly<CurrentHealth>(),
                    ComponentType.ReadOnly<CurrentEnergy>(),
                    ComponentType.ReadOnly<CurrentExperience>(),
                    ComponentType.ReadOnly<MaxHealth>(),
                    ComponentType.ReadOnly<MaxExperience>()
                );

                // Query do Inventário (Separada para garantir que não quebre a leitura de vida se o inventário não estiver presente instantaneamente)
                _garbageInventoryQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<GhostOwnerIsLocal>(),
                    ComponentType.ReadOnly<GarbageInventory>()
                );

                // Queries dos Singletons de Tempo e Rede
                _networkTimeQuery = em.CreateEntityQuery(typeof(NetworkTime));
                _tickRateQuery = em.CreateEntityQuery(typeof(ClientServerTickRate));

                break;
            }
        }
    }

    private void FindServertWorld()
    {
        foreach (var world in World.All)
        {
            if (world.IsServer())
            {
                _ServerWorld = world;
                var em = _ServerWorld.EntityManager;
                _waveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<WaveProperties>());
                break;
            }
        }
    }

    private void UpdateLocalPlayerData()
    {
        if (_localPlayerQuery == default || _localPlayerQuery.IsEmptyIgnoreFilter) return;

        if (_networkTimeQuery == default || _tickRateQuery == default) return;
        if (!_networkTimeQuery.HasSingleton<NetworkTime>() || !_tickRateQuery.HasSingleton<ClientServerTickRate>()) return;

        var networkTime = _networkTimeQuery.GetSingleton<NetworkTime>();
        var tickRateConfig = _tickRateQuery.GetSingleton<ClientServerTickRate>();

        var em = _clientWorld.EntityManager;

        using var entities = _localPlayerQuery.ToEntityArray(Allocator.Temp);
        var entity = entities[0];

        // Atualização de Energia
        if (em.HasComponent<CurrentEnergy>(entity))
        {
            var energy = em.GetComponentData<CurrentEnergy>(entity);
            UpdateEnergyPercentage(energy.value);
        }

        if (em.HasComponent<EnergyRestoreCooldown>(entity) && em.HasComponent<EnergyRestore>(entity))
        {
            var currentCooldown = em.GetComponentData<EnergyRestoreCooldown>(entity);
            var energyRestore = em.GetComponentData<EnergyRestore>(entity);
            var maxEnergy = em.GetComponentData<MaxEnergy>(entity);
            var currentEnergy = em.GetComponentData<CurrentEnergy>(entity);

            float maxSeconds = energyRestore.cooldownRestore;

            if (currentEnergy.value >= maxEnergy.value)
            {
                UpdateEnergyRestore(maxSeconds, maxSeconds);
            }
            else
            {
                int ticksRemaining = 0;

                if (currentCooldown.value.IsValid && currentCooldown.value.IsNewerThan(networkTime.ServerTick))
                {
                    ticksRemaining = currentCooldown.value.TicksSince(networkTime.ServerTick);
                }

                float remainingSeconds = ticksRemaining / (float)tickRateConfig.SimulationTickRate;
                UpdateEnergyRestore(maxSeconds - remainingSeconds, maxSeconds);
            }
        }

        if (em.HasComponent<HealthRegenCooldown>(entity) && em.HasComponent<HealthRegen>(entity))
        {
            var currentCooldown = em.GetComponentData<HealthRegenCooldown>(entity);
            var healthRestore = em.GetComponentData<HealthRegen>(entity);
            var maxHealth = em.GetComponentData<MaxHealth>(entity);
            var currentHealth = em.GetComponentData<CurrentHealth>(entity);

            float maxSeconds = healthRestore.cooldownRestore;

            if (currentHealth.value >= maxHealth.value)
            {
                UpdateHealthRestore(maxSeconds, maxSeconds);
            }
            else
            {
                int ticksRemaining = 0;

                if (currentCooldown.value.IsValid && currentCooldown.value.IsNewerThan(networkTime.ServerTick))
                {
                    ticksRemaining = currentCooldown.value.TicksSince(networkTime.ServerTick);
                }

                float remainingSeconds = ticksRemaining / (float)tickRateConfig.SimulationTickRate;
                UpdateHealthRestore(maxSeconds - remainingSeconds, maxSeconds);
            }
        }

        // Atualização de Vida
        if (em.HasComponent<CurrentHealth>(entity) && em.HasComponent<MaxHealth>(entity))
        {
            var health = em.GetComponentData<CurrentHealth>(entity);
            var maxHealth = em.GetComponentData<MaxHealth>(entity);
            UpdateRobotHealthPercentage(health.value, maxHealth.value);

            if (health.value <= 0) ShowLoseHUD();
            else if (LoseHUD.activeSelf) LoseHUD.SetActive(false);
        }

        // Atualização de XP
        if (em.HasComponent<CurrentExperience>(entity) && em.HasComponent<MaxExperience>(entity))
        {
            var exp = em.GetComponentData<CurrentExperience>(entity);
            var maxExp = em.GetComponentData<MaxExperience>(entity);
            UpdateExperienceBar(exp.value, maxExp.value);
        }

        if (_garbageInventoryQuery != default && !_garbageInventoryQuery.IsEmptyIgnoreFilter)
        {
            using var inventoryEntities = _garbageInventoryQuery.ToEntityArray(Allocator.Temp);
            var invEntity = inventoryEntities[0];

            if (em.HasComponent<GarbageInventory>(invEntity))
            {
                var inventory = em.GetComponentData<GarbageInventory>(invEntity);
                UpdateGarbageInventoryCounts(inventory.PlasticCount, inventory.PaperCount, inventory.GlassCount, inventory.MetalCount, inventory.OrganicCount, inventory.GarbageCount, inventory.MaxCapacityPerType);
            }
        }
    }

    private void UpdateVisualUpgradesVisibility()
    {
        if (AddUpgradesUIManager.Instance != null)
        {
            bool isChoosingUpgrade = AddUpgradesUIManager.Instance.gameObject.activeSelf;
            bool isHoldingButton = Input.GetKey(KeyCode.Tab);
            
            AddUpgradesUIManager.Instance.SetVisualUpgradesActive(isChoosingUpgrade || isHoldingButton);
        }
    }

    private void UpdateGlobalData()
    {
        if (_waveQuery == default || _waveQuery.IsEmptyIgnoreFilter) return;

        var em = _ServerWorld.EntityManager;
        using var waveEntities = _waveQuery.ToEntityArray(Allocator.Temp);

        var waveData = em.GetComponentData<WaveProperties>(waveEntities[0]);
        UpdateWaveCount(waveData.WaveCount);
    }

    // Métodos de UI
    public void UpdateEnergyPercentage(float value) => energyPercentageSlider.value = value;

    public void UpdateEnergyRestore(float cur, float max)
    {
        energyCooldownSlider.maxValue = max;
        energyCooldownSlider.value = cur;
    }

    public void UpdateHealthRestore(float cur, float max)
    {
        healthCooldownSlider.maxValue = max;
        healthCooldownSlider.value = cur;
    }

    public void UpdateRobotHealthPercentage(int cur, int max)
    {
        robotHealthPercentageSlider.value = cur;
        if (cur < 0) robotHealthPercentageSlider.value = 0;
        robotHealthPercentageSlider.maxValue = max;
    }

    public void UpdateExperienceBar(int cur, int max) { experienceBar.value = cur; experienceBar.maxValue = max; }
    public void UpdateWaveCount(int value) => waveCountText.text = $"Wave: {value}";

    public void UpdateGarbageInventoryCounts(int plastic, int paper, int glass, int metal, int organic, int total, int maxCapacity)
    {
        if (plasticText != null) plasticText.text = plastic.ToString();
        if (paperText != null) paperText.text = paper.ToString();
        if (glassText != null) glassText.text = glass.ToString();
        if (metalText != null) metalText.text = metal.ToString();
        if (organicText != null) organicText.text = organic.ToString();
        if (totalGarbageText != null) totalGarbageText.text = $"{total} / {maxCapacity}";

        UpdateTextColor(plasticText, plastic, maxCapacity);
        UpdateTextColor(paperText, paper, maxCapacity);
        UpdateTextColor(glassText, glass, maxCapacity);
        UpdateTextColor(metalText, metal, maxCapacity);
        UpdateTextColor(organicText, organic, maxCapacity);
        UpdateTextColor(totalGarbageText, total, maxCapacity);
    }

    private void UpdateTextColor(TextMeshProUGUI textComponent, int currentAmount, int maxCapacity)
    {
        if (textComponent == null) return;

        if (currentAmount >= maxCapacity)
            textComponent.color = Color.red;
        else if (currentAmount >= maxCapacity / 2f)
            textComponent.color = Color.yellow;
        else
            textComponent.color = Color.white;
    }

    public void ShowLoseHUD() => LoseHUD.SetActive(true);
}