using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AddUpgradesUIManager : MonoBehaviour
{
    [SerializeField]
    public GameObject[] GOUpgradesUI;

    [SerializeField]
    public UpgradeUIInfoSO[] upgradesUIInfo;
    public UpgradeUIInfoSO[] coreUpgradesUIInfo;

    [Header("Visual Upgrades HUD")]
    [SerializeField] private GameObject visualUpgradePrefab; // Prefab do item visual (Image + Texto de Count)
    [SerializeField] private Transform visualUpgradesParent; // Objeto pai (um Horizontal/Grid Layout Group)

    // Dicionário para guardar os objetos instanciados e suas quantidades
    private Dictionary<string, (GameObject go, int count)> activeVisualUpgrades = new Dictionary<string, (GameObject go, int count)>();

    public UpgradeLevel currentUpgradeLevel = UpgradeLevel.Commum;

    public static AddUpgradesUIManager Instance { get; private set; }

    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    void Start()
    {
        gameObject.SetActive(false);
    }

    public void ShowUpgrades(UpgradeLevel level)
    {
        gameObject.SetActive(true);
        currentUpgradeLevel = level;
        SetUpgradesInHUD();
    }

    World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            if (world.Name.Contains("ClientWorld"))
                return world;
        }
        return null;
    }

    void SetUpgradesInHUD()
    {
        var clientWorld = GetClientWorld();
        if (clientWorld == null) return;

        var em = clientWorld.EntityManager;

        EntityQuery playerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerInput>(),
            ComponentType.ReadOnly<GhostOwnerIsLocal>());

        if (playerQuery.IsEmptyIgnoreFilter) return;

        Entity clientLocalPlayerEntity;
        using (var players = playerQuery.ToEntityArray(Allocator.Temp))
        {
            clientLocalPlayerEntity = players.Length > 0 ? players[0] : Entity.Null;
        }
        if (clientLocalPlayerEntity == Entity.Null) return;

        EntityQuery globalEffectQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GlobalUpgradesPrefab>());
        if (globalEffectQuery.IsEmptyIgnoreFilter) return;

        Entity globalEffectEntity;
        using (var globals = globalEffectQuery.ToEntityArray(Allocator.Temp))
        {
            globalEffectEntity = globals.Length > 0 ? globals[0] : Entity.Null;
        }
        if (globalEffectEntity == Entity.Null) return;

        if (!em.HasBuffer<EffectPrefab>(clientLocalPlayerEntity)) return;
        if (!em.HasBuffer<GlobalUpgradesPrefab>(globalEffectEntity)) return;

        var playerEffects = em.GetBuffer<EffectPrefab>(clientLocalPlayerEntity);
        var globalEffects = em.GetBuffer<GlobalUpgradesPrefab>(globalEffectEntity);

        if (!CanAddEffects(playerEffects, globalEffects))
        {
            DisableAddEffects();
            return;
        }

        foreach (var item in GOUpgradesUI) item.SetActive(false);

        List<UpgradeUIInfoSO> AddEffectsInHUD = new List<UpgradeUIInfoSO>();
        foreach (var go in GOUpgradesUI)
        {
            var added = GetRandomEffect(playerEffects, globalEffects, AddEffectsInHUD);
            if (added != null) AddEffectsInHUD.Add(added);
        }

        for (int i = 0; i < AddEffectsInHUD.Count && i < GOUpgradesUI.Length; i++)
        {
            GOUpgradesUI[i].SetActive(true);
            SetEffectUI(AddEffectsInHUD[i], i);
        }
    }

    bool CanAddEffects(DynamicBuffer<EffectPrefab> playerEffects, DynamicBuffer<GlobalUpgradesPrefab> globalEffects)
    {
        if (globalEffects.IsEmpty) return false;
        if (playerEffects.IsEmpty) return true;

        for (int i = 0; i < globalEffects.Length; i++)
        {
            var globalEffectElement = globalEffects[i].Prefab;
            bool foundInPlayer = false;

            for (int j = 0; j < playerEffects.Length; j++)
            {
                if (globalEffectElement == playerEffects[j].Prefab)
                {
                    foundInPlayer = true;
                    break;
                }
            }

            if (!foundInPlayer) return true;
        }
        return false;
    }

    UpgradeUIInfoSO GetRandomEffect(DynamicBuffer<EffectPrefab> serverPlayerEffects, DynamicBuffer<GlobalUpgradesPrefab> globalEffects, List<UpgradeUIInfoSO> AddEffectsInHUD)
    {
        UpgradeUIInfoSO addEffectUI = null;
        if (globalEffects.IsEmpty) return null;

        HashSet<int> testados = new HashSet<int>();

        while (addEffectUI == null && testados.Count < globalEffects.Length)
        {
            int randomEffectValue = UnityEngine.Random.Range(0, globalEffects.Length);

            if (!testados.Add(randomEffectValue)) continue;

            var globalEffectName = globalEffects[randomEffectValue].Name.ToString();
            bool addThisEffect = true;

            foreach (var playerEffect in serverPlayerEffects)
            {
                if (globalEffectName == playerEffect.name)
                {
                    addThisEffect = false;
                    continue;
                }
            }

            if (addThisEffect)
            {
                foreach (var item in AddEffectsInHUD)
                {
                    if (globalEffectName == item.upgradeName)
                    {
                        addThisEffect = false;
                        break;
                    }
                }
            }

            if (addThisEffect)
            {
                if (currentUpgradeLevel == UpgradeLevel.Commum)
                {
                    foreach (var item in upgradesUIInfo)
                    {
                        if (item.upgradeName == globalEffectName)
                        {
                            addEffectUI = item;
                            break;
                        }
                    }
                }
                else
                {
                    foreach (var item in coreUpgradesUIInfo)
                    {
                        if (item.upgradeName == globalEffectName)
                        {
                            addEffectUI = item;
                            break;
                        }
                    }
                }
            }
        }
        return addEffectUI;
    }

    void SetEffectUI(UpgradeUIInfoSO upgrade, int goIndex)
    {
        var image = GOUpgradesUI[goIndex].transform.Find("Image").GetComponent<Image>();
        var description = GOUpgradesUI[goIndex].transform.Find("Description").GetComponent<TMP_Text>();
        var name = GOUpgradesUI[goIndex].transform.Find("Name").GetComponent<TMP_Text>();
        var button = GOUpgradesUI[goIndex].GetComponent<Button>();

        if (!image || !description || !name || !button) return;

        image.sprite = upgrade.image;
        description.text = upgrade.description;
        name.text = upgrade.upgradeName;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ChooseEffect(upgrade));
    }

    void ChooseEffect(UpgradeUIInfoSO upgrade)
    {
        var entityManager = GetClientWorld().EntityManager;
        var rpcEntity = entityManager.CreateEntity();

        if (upgrade.type == UpgradeType.Status)
        {
            entityManager.AddComponentData(rpcEntity, new ModifierStatusRpc { ModifierName = new FixedString64Bytes(upgrade.upgradeName) });
        }
        else if (upgrade.type == UpgradeType.Effect)
        {
            entityManager.AddComponentData(rpcEntity, new AddEffectRpc { EffectName = new FixedString64Bytes(upgrade.upgradeName) });
        }
        else
        {
            entityManager.AddComponentData(rpcEntity, new AddTechRpc { ComponentName = new FixedString64Bytes(upgrade.upgradeName) });
        }

        entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
        Debug.LogWarning($"rpcEnviado! {upgrade.upgradeName}");

        SetUpgradeVisualCount(upgrade);
        DisableAddEffects(); // Isso fecha a tela e aciona a trava de 1s do ECS automaticamente!
    }

    void DisableAddEffects()
    {
        for (int i = 0; i < GOUpgradesUI.Length; i++)
        {
            GOUpgradesUI[i].SetActive(false);
        }
        gameObject.SetActive(false);
    }

    void SetUpgradeVisualCount(UpgradeUIInfoSO upgrade)
    {
        if (activeVisualUpgrades.ContainsKey(upgrade.upgradeName))
        {
            // Já existe na tela, só atualiza o número
            var data = activeVisualUpgrades[upgrade.upgradeName];
            data.count++;
            data.go.GetComponentInChildren<TMP_Text>().text = data.count.ToString();
            activeVisualUpgrades[upgrade.upgradeName] = data; // Salva de volta no dicionário
        }
        else
        {
            // Primeira vez, cria do zero
            GameObject newVisual = Instantiate(visualUpgradePrefab, visualUpgradesParent);
            
            // Pega a imagem (se estiver num filho chamado "Image") e o texto (no próprio objeto ou filhos)
            var image = newVisual.transform.Find("Image")?.GetComponent<Image>();
            if (image != null) image.sprite = upgrade.image;
            
            var countText = newVisual.GetComponentInChildren<TMP_Text>();
            if (countText != null) countText.text = "1";

            activeVisualUpgrades.Add(upgrade.upgradeName, (newVisual, 1));
        }
    }

    public void DisableAllUpgradesVisual()
    {
        foreach (var kvp in activeVisualUpgrades)
        {
            if (kvp.Value.go != null)
                Destroy(kvp.Value.go);
        }
        activeVisualUpgrades.Clear();
    }
}