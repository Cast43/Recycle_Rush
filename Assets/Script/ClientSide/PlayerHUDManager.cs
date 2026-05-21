using UnityEngine;
using TMPro;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

public class PlayerHUDManager : MonoBehaviour
{
    public static PlayerHUDManager Instance;

    private World _clientWorld;

    private EntityQuery _localPlayerQuery;
    private EntityQuery _waveQuery;
    private EntityQuery _networkTimeQuery;
    private EntityQuery _tickRateQuery;
    private EntityQuery _eventQuery;

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

    [Header("Event UI")]
    [SerializeField] private GameObject eventPanel;
    [SerializeField] private TextMeshProUGUI eventNameText;
    [SerializeField] private UnityEngine.UI.Slider eventProgressSlider;
    [SerializeField] private TextMeshProUGUI eventProgressText;

    [Header("Event Texts Config")]
    [SerializeField] private EventUIInfoSO[] eventUIInfos;

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

                // Query do Player - usa PlayerInput e GhostOwnerIsLocal para achar inequivocamente o avatar local
                _localPlayerQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<GhostOwnerIsLocal>(),
                    ComponentType.ReadOnly<PlayerInput>()
                );

                // Queries dos Singletons de Tempo e Rede
                _networkTimeQuery = em.CreateEntityQuery(typeof(NetworkTime));
                _tickRateQuery = em.CreateEntityQuery(typeof(ClientServerTickRate));

                _eventQuery = em.CreateEntityQuery(ComponentType.ReadOnly<EventObjective>(), ComponentType.ReadOnly<EventActiveTag>());
                _waveQuery = em.CreateEntityQuery(ComponentType.ReadOnly<WaveProperties>());

                break;
            }
        }
    }

    private void UpdateLocalPlayerData()
    {
        if (_localPlayerQuery == default || _localPlayerQuery.IsEmptyIgnoreFilter) return;

        var em = _clientWorld.EntityManager;

        using var entities = _localPlayerQuery.ToEntityArray(Allocator.Temp);
        if (entities.Length == 0) return;
        var entity = entities[0];

        bool hasTimeSingletons = _networkTimeQuery != default && _tickRateQuery != default &&
                                 _networkTimeQuery.HasSingleton<NetworkTime>() && _tickRateQuery.HasSingleton<ClientServerTickRate>();

        NetworkTime networkTime = default;
        ClientServerTickRate tickRateConfig = default;

        if (hasTimeSingletons)
        {
            networkTime = _networkTimeQuery.GetSingleton<NetworkTime>();
            tickRateConfig = _tickRateQuery.GetSingleton<ClientServerTickRate>();
        }

        // Atualização de Energia
        if (em.HasComponent<CurrentEnergy>(entity))
        {
            var energy = em.GetComponentData<CurrentEnergy>(entity);
            UpdateEnergyPercentage(energy.value);
        }

        if (hasTimeSingletons && em.HasComponent<EnergyRestoreCooldown>(entity) && em.HasComponent<EnergyRestore>(entity) && em.HasComponent<CurrentEnergy>(entity) && em.HasComponent<MaxEnergy>(entity))
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

        if (hasTimeSingletons && em.HasComponent<HealthRegenCooldown>(entity) && em.HasComponent<HealthRegen>(entity) && em.HasComponent<CurrentHealth>(entity) && em.HasComponent<MaxHealth>(entity))
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

        if (em.HasComponent<GarbageInventory>(entity))
        {
            var inventory = em.GetComponentData<GarbageInventory>(entity);
            UpdateGarbageInventoryCounts(inventory.PlasticCount, inventory.PaperCount, inventory.GlassCount, inventory.MetalCount, inventory.OrganicCount, inventory.GarbageCount, inventory.MaxCapacityPerType);
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
        var emClient = _clientWorld.EntityManager;

        if (_waveQuery != default && !_waveQuery.IsEmptyIgnoreFilter)
        {
            using var waveEntities = _waveQuery.ToEntityArray(Allocator.Temp);
            if (waveEntities.Length > 0)
            {
                var waveData = emClient.GetComponentData<WaveProperties>(waveEntities[0]);
                UpdateWaveCount(waveData.WaveCount);
            }
        }
        
        if (_eventQuery != default && !_eventQuery.IsEmptyIgnoreFilter)
        {
            using var eventEntities = _eventQuery.ToEntityArray(Allocator.Temp);
            if (eventEntities.Length > 0)
            {
                var eventObjective = emClient.GetComponentData<EventObjective>(eventEntities[0]);
                
                if (eventPanel != null && !eventPanel.activeSelf) eventPanel.SetActive(true);
                
                if (eventNameText != null) 
                {
                    eventNameText.text = "Evento Ativo"; // Valor padrão
                    foreach (var info in eventUIInfos)
                    {
                        if (info != null && info.eventType == eventObjective.Type)
                        {
                            eventNameText.text = info.eventName;
                            break;
                        }
                    }
                }

                if (eventProgressSlider != null)
                {
                    eventProgressSlider.maxValue = eventObjective.TargetValue;
                    eventProgressSlider.value = eventObjective.Progress;
                }

                if (eventProgressText != null)
                {
                    eventProgressText.text = "Complete o objetivo do evento!"; // Valor padrão
                    foreach (var info in eventUIInfos)
                    {
                        if (info != null && info.eventType == eventObjective.Type)
                        {
                            eventProgressText.text = info.eventDescription;
                            break;
                        }
                    }
                }
            }
        }
        else
        {
            if (eventPanel != null && eventPanel.activeSelf) eventPanel.SetActive(false);
        }
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