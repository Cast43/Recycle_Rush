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

    [System.Serializable]
    public class UpgradeUIInfo
    {
        public string name;
        public string description;
        public Sprite image;
        public UpgradeType type;
    }

    [SerializeField]
    public UpgradeUIInfo[] upgradesUIInfo;
    public UpgradeUIInfo[] coreUpgradesUIInfo;

    [SerializeField]
    public GOVisualUpgradesHUD[] GOVisualUpgrades;

    [System.Serializable]
    public class GOVisualUpgradesHUD
    {
        public GameObject GOVisual;
        public int count;
    }

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

        List<UpgradeUIInfo> AddEffectsInHUD = new List<UpgradeUIInfo>();
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

    UpgradeUIInfo GetRandomEffect(DynamicBuffer<EffectPrefab> serverPlayerEffects, DynamicBuffer<GlobalUpgradesPrefab> globalEffects, List<UpgradeUIInfo> AddEffectsInHUD)
    {
        UpgradeUIInfo addEffectUI = null;
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
                    if (globalEffectName == item.name)
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
                        if (item.name == globalEffectName)
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
                        if (item.name == globalEffectName)
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

    void SetEffectUI(UpgradeUIInfo upgrade, int goIndex)
    {
        var image = GOUpgradesUI[goIndex].transform.Find("Image").GetComponent<Image>();
        var description = GOUpgradesUI[goIndex].transform.Find("Description").GetComponent<TMP_Text>();
        var name = GOUpgradesUI[goIndex].transform.Find("Name").GetComponent<TMP_Text>();
        var button = GOUpgradesUI[goIndex].GetComponent<Button>();

        if (!image || !description || !name || !button) return;

        image.sprite = upgrade.image;
        description.text = upgrade.description;
        name.text = upgrade.name;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ChooseEffect(upgrade));
    }

    void ChooseEffect(UpgradeUIInfo upgrade)
    {
        var entityManager = GetClientWorld().EntityManager;
        var rpcEntity = entityManager.CreateEntity();

        if (upgrade.type == UpgradeType.Status)
        {
            entityManager.AddComponentData(rpcEntity, new ModifierStatusRpc { ModifierName = new FixedString64Bytes(upgrade.name) });
        }
        else if (upgrade.type == UpgradeType.Effect)
        {
            entityManager.AddComponentData(rpcEntity, new AddEffectRpc { EffectName = new FixedString64Bytes(upgrade.name) });
        }
        else
        {
            entityManager.AddComponentData(rpcEntity, new AddComponentRpc { ComponentName = new FixedString64Bytes(upgrade.name) });
        }

        entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
        Debug.LogWarning($"rpcEnviado! {upgrade.name}");

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

    void SetUpgradeVisualCount(UpgradeUIInfo upgrade)
    {
        foreach (var visual in GOVisualUpgrades)
        {
            if (visual.GOVisual.name == upgrade.name)
            {
                visual.GOVisual.SetActive(true);
                visual.count++;
                visual.GOVisual.GetComponentInChildren<TMP_Text>().text = visual.count.ToString();
            }
        }
    }

    public void DisableAllUpgradesVisual()
    {
        foreach (var visual in GOVisualUpgrades)
        {
            visual.GOVisual.SetActive(false);
            visual.count = 0;
        }
    }
}