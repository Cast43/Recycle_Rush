using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
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

    public void ShowUpgrades(UpgradeLevel level)
    {
        currentUpgradeLevel = level;
        SetUpgradesInHUD();
    }
    // void OnEnable()
    // {
    //     SetUpgradesInHUD();
    // }
    //pega o mundo do servidor para encontrar o playerLocal
    World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            // NetCode cria mundos com "ClientWorld" no nome
            if (world.Name.Contains("ClientWorld"))
                return world;
        }
        return null;
    }
    World GetServerWorld()
    {
        foreach (var world in World.All)
        {
            if (world.Name.Contains("ServerWorld"))
                return world;
        }
        return null;
    }

    void SetUpgradesInHUD()
    {
        var clientWorld = GetClientWorld();
        if (clientWorld == null)
        {
            Debug.LogWarning("ClientWorld não encontrada ainda.");
            return;
        }

        var em = clientWorld.EntityManager;

        // Encontra o player local usando GhostOwnerIsLocal (método robusto)
        EntityQuery playerQuery = em.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerInput>(),
            ComponentType.ReadOnly<GhostOwnerIsLocal>());

        if (playerQuery.IsEmptyIgnoreFilter)
        {
            Debug.LogWarning("Nenhum player local encontrado (GhostOwnerIsLocal).");
            return;
        }

        Entity clientLocalPlayerEntity;
        using (var players = playerQuery.ToEntityArray(Allocator.Temp))
        {
            clientLocalPlayerEntity = players.Length > 0 ? players[0] : Entity.Null;
        }
        if (clientLocalPlayerEntity == Entity.Null)
        {
            Debug.LogWarning("Não encontrei o player local para atualizar HUD.");
            return;
        }

        // Busca GlobalEffectPrefab no client world (deve vir como ghost)
        EntityQuery globalEffectQuery = em.CreateEntityQuery(ComponentType.ReadOnly<GlobalUpgradesPrefab>());
        if (globalEffectQuery.IsEmptyIgnoreFilter)
        {
            Debug.LogWarning("Nenhuma entidade GlobalEffectPrefab encontrada no client world.");
            return;
        }

        Entity globalEffectEntity;
        using (var globals = globalEffectQuery.ToEntityArray(Allocator.Temp))
        {
            globalEffectEntity = globals.Length > 0 ? globals[0] : Entity.Null;
        }
        if (globalEffectEntity == Entity.Null)
        {
            Debug.LogWarning("GlobalEffect entity não encontrada.");
            return;
        }

        // Confere buffers
        if (!em.HasBuffer<EffectPrefab>(clientLocalPlayerEntity))
        {
            Debug.LogWarning("Player local não tem buffer EffectPrefab.");
            return;
        }
        if (!em.HasBuffer<GlobalUpgradesPrefab>(globalEffectEntity))
        {
            Debug.LogWarning("GlobalEffect entity não tem buffer GlobalEffectPrefab.");
            return;
        }

        var playerEffects = em.GetBuffer<EffectPrefab>(clientLocalPlayerEntity);
        var globalEffects = em.GetBuffer<GlobalUpgradesPrefab>(globalEffectEntity);

        if (!CanAddEffects(playerEffects, globalEffects))
        {
            Debug.LogWarning("Não há mais efeitos para adicionar");
            DisableAddEffects();
            return;
        }

        // Limpa UI e monta opções
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
        // Se não há efeitos globais, não há nada para adicionar
        if (globalEffects.IsEmpty)
            return false;

        // Se o player não tem efeitos, podemos adicionar qualquer um
        if (playerEffects.IsEmpty)
            return true;

        // Para cada efeito global...
        for (int i = 0; i < globalEffects.Length; i++)
        {
            var globalEffectElement = globalEffects[i].Prefab;
            bool foundInPlayer = false;

            // Verifica se já está no player
            for (int j = 0; j < playerEffects.Length; j++)
            {
                if (globalEffectElement == playerEffects[j].Prefab)
                {
                    foundInPlayer = true;
                    break; // já achou, não precisa continuar comparando esse
                }
            }

            // Se não foi encontrado no player, ainda há efeito para adicionar
            if (!foundInPlayer)
                return true;
        }

        // Todos os efeitos globais já estão no player
        return false;
    }

    //verificar efeitos que ja estão no player para não adicionar
    UpgradeUIInfo GetRandomEffect(DynamicBuffer<EffectPrefab> serverPlayerEffects, DynamicBuffer<GlobalUpgradesPrefab> globalEffects,
                                List<UpgradeUIInfo> AddEffectsInHUD)
    {
        UpgradeUIInfo addEffectUI = null;

        if (globalEffects.IsEmpty)
        {
            Debug.LogWarning("Não há efeitos no GlobalEffects");
            return null;
        }

        HashSet<int> testados = new HashSet<int>();

        while (addEffectUI == null && testados.Count < globalEffects.Length)
        {
            int randomEffectValue = UnityEngine.Random.Range(0, globalEffects.Length);

            // Evita repetir o mesmo índice
            if (!testados.Add(randomEffectValue))
                continue;

            var globalEffectName = globalEffects[randomEffectValue].Name.ToString();
            // var globalEffectPrefab = globalEffects[randomEffectValue].Prefab;


            bool addThisEffect = true;

            // Verifica se o efeito já está no player
            foreach (var playerEffect in serverPlayerEffects)
            {
                // Debug.Log($"Passando pelo efeito global: {globalEffectName} e player {playerEffect.name}");
                if (globalEffectName == playerEffect.name)
                {
                    // Debug.Log($"Efeito já no player: {globalEffectName}");
                    addThisEffect = false;
                    continue;
                }
            }

            // Verifica se o efeito já está na HUD
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

            // Se for válido, procura no effectsUIInfo
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

        if (!image)
        {
            Debug.LogWarning("precisa de um objeto imagem na effect UI"); return;
        }
        if (!description)
        {
            Debug.LogWarning("precisa de um objeto description na effect UI"); return;
        }
        if (!name)
        {
            Debug.LogWarning("precisa de um objeto nome na effect UI"); return;
        }
        if (!button)
        {
            Debug.LogWarning("precisa de um componente Button no objeto para adicionar onClick");
            return;
        }

        image.sprite = upgrade.image;
        description.text = upgrade.description;
        name.text = upgrade.name;

        // Remove listeners antigos para evitar duplicação
        button.onClick.RemoveAllListeners();

        // // Captura o nome do efeito para usar no listener
        // string upgradeName = effect.name;

        // Adiciona o listener, chamando chooseEffects com o nome
        button.onClick.AddListener(() => ChooseEffect(upgrade));
    }
    void ChooseEffect(UpgradeUIInfo upgrade)
    {
        var entityManager = GetClientWorld().EntityManager;

        // Cria a entidade RPC
        var rpcEntity = entityManager.CreateEntity();

        // Procura a entidade que representa a conexão com o servidor
        var query = entityManager.CreateEntityQuery(typeof(NetworkStreamConnection));
        var connectionEntity = Entity.Null;
        if (query.CalculateEntityCount() > 0)
        {
            connectionEntity = query.GetSingletonEntity();
            // Debug.Log($"Entidade de conexão encontrada: {connectionEntity}");
        }
        else
        {
            Debug.LogWarning("Nenhuma conexão encontrada ainda!");
        }

        // Adiciona o componente do RPC
        if (upgrade.type == UpgradeType.Status)
        {
            entityManager.AddComponentData(rpcEntity, new ModifierStatusRpc
            {
                ModifierName = new FixedString64Bytes(upgrade.name)
            });
        }
        else if (upgrade.type == UpgradeType.Effect)
        {
            entityManager.AddComponentData(rpcEntity, new AddEffectRpc
            {
                EffectName = new FixedString64Bytes(upgrade.name)
            });
        }
        else
        {
            entityManager.AddComponentData(rpcEntity, new AddComponentRpc
            {
                ComponentName = new FixedString64Bytes(upgrade.name)
            });
        }

        entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
        // Adiciona SendRpcCommandRequest com TargetConnection
        // entityManager.AddComponentData(rpcEntity, new ConnectionEntity { Value = connectionEntity });
        Debug.LogWarning($"rpcEnviado!{upgrade.name}");

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

        //envia um RPC para diminuir upgrades pending 
        var entityManager = GetClientWorld().EntityManager;
        var rpcEntity = entityManager.CreateEntity();

        entityManager.AddComponentData(rpcEntity, new DecreaseUpgradesPendingRpc { });

        entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
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
