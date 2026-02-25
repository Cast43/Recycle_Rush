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

    [SerializeField] private TextMeshProUGUI energyPercentageText;
    [SerializeField] private TextMeshProUGUI robotHealthPercentageText;
    [SerializeField] private TextMeshProUGUI waveCountText;
    [SerializeField] private UnityEngine.UI.Slider experienceBar;
    [SerializeField] private GameObject LoseHUD;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Update()
    {
        // Se o mundo ainda não existe ou foi destruído, tentamos encontrar
        if (_clientWorld == null || !_clientWorld.IsCreated)
        {
            FindClientWorld();
            return; // Sai do Update para esperar o próximo frame
        }
        if (_ServerWorld == null || !_ServerWorld.IsCreated)
        {
            FindServertWorld();
            return; // Sai do Update para esperar o próximo frame
        }

        // Só rodamos a lógica se as queries foram criadas com sucesso
        UpdateLocalPlayerData();
        UpdateGlobalData();
    }

    private void FindClientWorld()
    {
        // Varre todos os mundos ativos
        foreach (var world in World.All)
        {
            // Verifica se é o mundo do Cliente (não o Server nem o ThinClient)
            if (world.IsClient() && !world.IsThinClient())
            {
                _clientWorld = world;
                var em = _clientWorld.EntityManager;

                // CRIAMOS AS QUERIES AQUI, usando a EntityManager deste mundo específico
                _localPlayerQuery = em.CreateEntityQuery(
                    ComponentType.ReadOnly<GhostOwnerIsLocal>(),
                    ComponentType.ReadOnly<CurrentHealth>(),
                    ComponentType.ReadOnly<CurrentEnergy>(),
                    ComponentType.ReadOnly<CurrentExperience>(),
                    ComponentType.ReadOnly<MaxHealth>(),
                    ComponentType.ReadOnly<MaxExperience>()
                );

                break;
            }
        }
    }

    private void FindServertWorld()
    {
        // Varre todos os mundos ativos
        foreach (var world in World.All)
        {
            // Verifica se é o mundo do Cliente (não o Server nem o ThinClient)
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
        // Se a query for nula ou não encontrar ninguém, paramos aqui
        if (_localPlayerQuery == default || _localPlayerQuery.IsEmptyIgnoreFilter) return;

        var em = _clientWorld.EntityManager;

        // Pegamos a entidade do player local
        using var entities = _localPlayerQuery.ToEntityArray(Allocator.Temp);
        var entity = entities[0];

        // Atualização de Energia
        if (em.HasComponent<CurrentEnergy>(entity))
        {
            var energy = em.GetComponentData<CurrentEnergy>(entity);
            UpdateEnergyPercentage(energy.value);
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
    }

    private void UpdateGlobalData()
    {
        if (_waveQuery == default || _waveQuery.IsEmptyIgnoreFilter) return;

        var em = _ServerWorld.EntityManager;
        using var waveEntities = _waveQuery.ToEntityArray(Allocator.Temp);

        var waveData = em.GetComponentData<WaveProperties>(waveEntities[0]);
        UpdateWaveCount(waveData.WaveCount);
    }

    // Métodos de UI permanecem os mesmos...
    public void UpdateEnergyPercentage(float value) => energyPercentageText.text = $"{value:0}%";
    public void UpdateRobotHealthPercentage(int cur, int max)
    {
        robotHealthPercentageText.text = $"{cur}/{max}";
        if (cur < 0)
        {
            robotHealthPercentageText.text = $"{0}/{max}";
        }
    }
    public void UpdateExperienceBar(int cur, int max) { experienceBar.value = cur; experienceBar.maxValue = max; }
    public void UpdateWaveCount(int value) => waveCountText.text = $"Wave: {value}";
    public void ShowLoseHUD() => LoseHUD.SetActive(true);
}