using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System;
public class AddEffectUIManager : MonoBehaviour
{
    [SerializeField]
    public GameObject[] GOEffectsUI;

    [System.Serializable]
    public class EffectUIInfo
    {
        public string name;
        public string description;
        public Sprite image;
    }

    [SerializeField]
    public EffectUIInfo[] effectsUIInfo;

    void OnEnable()
    {
        SetEffectInHUD();
    }
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
    void SetEffectInHUD()
    {
        //seta o mundo do player e do jogador
        //-pega os efeitos no localplayer
        //-pegar os efeitos globais do servidor
        var world = World.DefaultGameObjectInjectionWorld;

        //modificar para adicionar o efeito para o cliente também
        var clientLocalPlayer = GetServerWorld().EntityManager;
        var globalEffectsReference = GetServerWorld().EntityManager;

        // Pega o NetworkId do cliente local
        var netId = clientLocalPlayer.CreateEntityQuery(typeof(NetworkId)).GetSingleton<NetworkId>().Value;

        // Procura a entidade do player local
        EntityQuery playerQuery = clientLocalPlayer.CreateEntityQuery(
            ComponentType.ReadOnly<PlayerInput>(),
            ComponentType.ReadOnly<GhostOwner>()
        );

        EntityQuery GlobalEffectQuerry = globalEffectsReference.CreateEntityQuery(
        ComponentType.ReadOnly<GlobalEffectPrefab>()
        );
        Entity clientLocalPlayerEntity = Entity.Null;
        Entity globalEffectEntity = Entity.Null;
        //encotra o player local
        using (var players = playerQuery.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in players)
            {
                if (clientLocalPlayer.GetComponentData<GhostOwner>(entity).NetworkId == netId)
                {
                    clientLocalPlayerEntity = entity;
                    break;
                }
            }
        }
        using (var globalEffect = GlobalEffectQuerry.ToEntityArray(Allocator.Temp))
        {
            foreach (var entity in globalEffect)
            {
                globalEffectEntity = entity;
                break;
            }
        }
        if (globalEffectEntity == Entity.Null)
        {
            Debug.LogWarning("Não encontrei o player local para atualizar HUD.");
            return;
        }
        if (clientLocalPlayerEntity == Entity.Null)
        {
            Debug.LogWarning("Não encontrei os efeitos globais para atualizar HUD.");
            return;
        }
        // Lê o buffer de efeitos
        if (!clientLocalPlayer.HasBuffer<EffectPrefab>(clientLocalPlayerEntity))
        {
            Debug.LogWarning("Não encontrei o buffer de EffectPrefab.");
            return;
        }
        if (!globalEffectsReference.HasBuffer<GlobalEffectPrefab>(globalEffectEntity))
        {
            Debug.LogWarning("Não encontrei o buffer de GlobalEffectPrefab.");
            return;
        }
        var playerEffects = clientLocalPlayer.GetBuffer<EffectPrefab>(clientLocalPlayerEntity);
        var globalEffects = globalEffectsReference.GetBuffer<GlobalEffectPrefab>(globalEffectEntity);

        //verifica se possui efeitos para adicionar 
        if (!CanAddEffects(playerEffects, globalEffects))
        {
            Debug.LogWarning("Não há mais efeitos para adicionar");
            DisableAddEffects();
            return;
        }
        foreach (var item in playerEffects)
        {
            Debug.Log(item.name);
        }

        //cria uma lista para ver quais efeitos serão adicionados
        List<EffectUIInfo> AddEffectsInHUD = new List<EffectUIInfo>();
        //consegue o efeito randomico
        foreach (var item in GOEffectsUI)
        {
            item.SetActive(false);
            var AddedEffectUIElement = GetRandomEffect(playerEffects, globalEffects, AddEffectsInHUD);
            if (AddedEffectUIElement != null)
            {
                AddEffectsInHUD.Add(AddedEffectUIElement);
                // Debug.Log(AddedEffectUIElement.name);
            }
        }
        for (int i = 0; i < AddEffectsInHUD.Count; i++)
        {
            GOEffectsUI[i].SetActive(true);
            SetEffectUI(AddEffectsInHUD[i], i);
        }
        AddEffectsInHUD = null;
    }
    bool CanAddEffects(DynamicBuffer<EffectPrefab> playerEffects, DynamicBuffer<GlobalEffectPrefab> globalEffects)
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
    EffectUIInfo GetRandomEffect(DynamicBuffer<EffectPrefab> serverPlayerEffects, DynamicBuffer<GlobalEffectPrefab> globalEffects,
                                List<EffectUIInfo> AddEffectsInHUD)
    {
        EffectUIInfo addEffectUI = null;

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

            var globalEffectName = globalEffects[randomEffectValue].name.ToString();
            var globalEffectPrefab = globalEffects[randomEffectValue].Prefab;

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
                foreach (var item in effectsUIInfo)
                {
                    if (item.name == globalEffectName)
                    {
                        addEffectUI = item;
                        break;
                    }
                }
            }
        }

        return addEffectUI;
    }


    void SetEffectUI(EffectUIInfo effect, int goIndex)
    {
        var image = GOEffectsUI[goIndex].transform.Find("Image").GetComponent<Image>();
        var description = GOEffectsUI[goIndex].transform.Find("Description").GetComponent<TMP_Text>();
        var name = GOEffectsUI[goIndex].transform.Find("Name").GetComponent<TMP_Text>();
        var button = GOEffectsUI[goIndex].GetComponent<Button>();

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

        image.sprite = effect.image;
        description.text = effect.description;
        name.text = effect.name;

        // Remove listeners antigos para evitar duplicação
        button.onClick.RemoveAllListeners();

        // Captura o nome do efeito para usar no listener
        string effectName = effect.name;

        // Adiciona o listener, chamando chooseEffects com o nome
        button.onClick.AddListener(() => ChooseEffect(effectName));
    }
    void ChooseEffect(string effectName)
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
        entityManager.AddComponentData(rpcEntity, new AddEffectRpc
        {
            EffectName = new FixedString64Bytes(effectName)
        });

        entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);
        // Adiciona SendRpcCommandRequest com TargetConnection
        // entityManager.AddComponentData(rpcEntity, new ConnectionEntity { Value = connectionEntity });
        Debug.LogWarning($"rpcEnviado!{effectName}");


        DisableAddEffects();
    }
    void DisableAddEffects()
    {
        for (int i = 0; i < GOEffectsUI.Length; i++)
        {
            GOEffectsUI[i].SetActive(false);
        }
        gameObject.SetActive(false);
    }


}
