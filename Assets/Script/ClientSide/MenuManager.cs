using UnityEngine;
using UnityEngine.UI;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.SceneManagement;
using System.Linq;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;

public class MenuManager : MonoBehaviour
{
    public GameObject menuPanel;
    bool isEnabled = false;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        menuPanel.SetActive(isEnabled);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isEnabled)
            {
                isEnabled = false;
            }
            else
            {
                isEnabled = true;
            }
            menuPanel.SetActive(isEnabled);
        }
    }
    public void LeaveMatch()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;

        // detecta se o mundo atual tem os grupos de simulação
        bool isServer = world.IsServer();
        bool isClient = world.IsClient();

        if (isClient && !isServer)
        {
            // cliente: pede desconexão
            DisconnectClient();
        }
        else if (isServer)
        {
            // servidor: desconecta todos e encerra mundos
            DisconnectAllClientsOnServer();
            StopAllAndCleanup();
        }
        else
        {
            // fallback: tenta desconectar como cliente
            DisconnectClient();
        }
        SceneManager.LoadScene(0, LoadSceneMode.Single);
    }

    public void DisconnectClient()
    {
        var world = World.DefaultGameObjectInjectionWorld;
        if (world == null) return;
        var em = world.EntityManager;
        var query = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
        if (query.IsEmpty) return;

        var connEntity = query.GetSingletonEntity();
        // já existe um pedido de disconnect?
        if (!em.HasComponent<NetworkStreamRequestDisconnect>(connEntity))
        {
            em.AddComponentData(connEntity, new NetworkStreamRequestDisconnect());
            Debug.Log("Requesting client disconnect");
        }
    }

    // pede desconexão de todas as connections do servidor (marca todas as NetworkStreamConnection)
    public void DisconnectAllClientsOnServer()
    {
        // iterar diretamente sobre World.All evita o boxing causado por ToArray()/LINQ
        foreach (var world in World.All)
        {
            if (!world.IsCreated) continue;
            // trata apenas mundos de simulação/netcode (ajuste conforme sua arquitetura)
            if ((world.Flags & WorldFlags.Game) == 0) continue;

            var em = world.EntityManager;
            var query = em.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamConnection>());
            if (query.IsEmpty) continue;

            using (var entities = query.ToEntityArray(Unity.Collections.Allocator.Temp))
            {
                foreach (var e in entities)
                {
                    if (!em.HasComponent<NetworkStreamRequestDisconnect>(e))
                        em.AddComponentData(e, new NetworkStreamRequestDisconnect());
                }
            }
        }
        Debug.Log("Requested disconnect to all connections on server worlds.");
    }

    // para completamente (fecha mundos cliente/servidor criados pelo StartHost/StartClient)
    public void StopAllAndCleanup()
    {
        // Colete as referências dos mundos sem usar LINQ/ToArray (evita boxing)
        var worlds = new System.Collections.Generic.List<World>(10);
        foreach (var w in World.All)
        {
            worlds.Add(w);
        }

        foreach (var w in worlds)
        {
            if (w == null) continue;
            if (!w.IsCreated) continue;
            // opcional: filtre apenas mundos que você criou (ServerWorld/ClientWorld)
            if ((w.Flags & WorldFlags.Game) == 0) continue;

            try
            {
                Debug.Log($"Disposing world {w.Name}");
                w.Dispose();
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"Erro ao descartar world {w.Name}: {ex}");
            }
        }

        // limpa referência padrão
        if (World.DefaultGameObjectInjectionWorld != null)
            World.DefaultGameObjectInjectionWorld = null;

        Debug.Log("Stopped worlds and cleaned up.");
    }

    public void RestartScene()
    {
        // 1. Encontra o Mundo do Cliente (Onde o jogador está clicando)
        World clientWorld = GetClientWorld();

        if (clientWorld != null)
        {
            // 2. Cria uma entidade temporária para carregar o RPC
            var entityManager = clientWorld.EntityManager;
            var rpcEntity = entityManager.CreateEntity();

            // 3. Adiciona o componente do RPC
            entityManager.AddComponent<RestartGameRpc>(rpcEntity);

            // 4. Adiciona o pedido de envio para o Servidor
            entityManager.AddComponent<SendRpcCommandRequest>(rpcEntity);

            Debug.Log("Pedido de reinício enviado ao servidor!");
        }
        else
        {
            Debug.LogError("Não foi possível encontrar o Mundo do Cliente!");
        }
    }

    // Função auxiliar para achar o mundo correto
    private World GetClientWorld()
    {
        foreach (var world in World.All)
        {
            // Pega o primeiro mundo que é Cliente e não é Servidor (ou é Host)
            if (world.IsClient()) return world;
        }
        return null;
    }

}
