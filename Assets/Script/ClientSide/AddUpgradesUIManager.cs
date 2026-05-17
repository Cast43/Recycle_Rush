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
    [SerializeField] private GameObject visualUpgradePrefab;
    [SerializeField] private Transform visualUpgradesParent;

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
        GameObject uiParent = GOUpgradesUI[goIndex];
        Image image = null;
        TMP_Text description = null;
        TMP_Text nameText = null;
        var button = uiParent.GetComponent<Button>();

        foreach (var img in uiParent.GetComponentsInChildren<Image>(true))
        {
            if (img.gameObject.name == "Image" || img.gameObject.name == "Icon") { image = img; break; }
        }
        if (image == null)
        {
            var imgs = uiParent.GetComponentsInChildren<Image>(true);
            image = imgs.Length > 1 ? imgs[1] : (imgs.Length > 0 ? imgs[0] : null);
        }

        foreach (var txt in uiParent.GetComponentsInChildren<TMP_Text>(true))
        {
            if (txt.gameObject.name == "Description") description = txt;
            if (txt.gameObject.name == "Name") nameText = txt;
        }

        if (image == null || description == null || nameText == null || button == null)
        {
            Debug.LogError($"[AddUpgradesUIManager] Falha na UI '{uiParent.name}'. Certifique-se de que os nomes dos textos são 'Name' e 'Description', e que exista um 'Image'.");
            return;
        }

        image.sprite = upgrade.image;
        description.text = upgrade.description;
        nameText.text = upgrade.upgradeName;

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
        DisableAddEffects();
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
            var data = activeVisualUpgrades[upgrade.upgradeName];
            data.count++;
            data.go.GetComponentInChildren<TMP_Text>().text = data.count.ToString();
            activeVisualUpgrades[upgrade.upgradeName] = data;
        }
        else
        {
            GameObject newVisual = Instantiate(visualUpgradePrefab, visualUpgradesParent);
            
            Image image = null;
            foreach (var img in newVisual.GetComponentsInChildren<Image>(true))
            {
                if (img.gameObject.name == "Image" || img.gameObject.name == "Icon") { image = img; break; }
            }
            if (image == null) 
            {
                var imgs = newVisual.GetComponentsInChildren<Image>(true);
                image = imgs.Length > 1 ? imgs[1] : (imgs.Length > 0 ? imgs[0] : null);
            }
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

    public void SetVisualUpgradesActive(bool isActive)
    {
        if (visualUpgradesParent != null && visualUpgradesParent.gameObject.activeSelf != isActive)
        {
            visualUpgradesParent.gameObject.SetActive(isActive);
        }
    }
}