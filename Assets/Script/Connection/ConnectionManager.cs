using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Lobbies;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeTMP;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private bool isServerBuild = false;

    private static RelayServerData relayServerData;
    private static RelayServerData relayClientData;

    async void Awake()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log("Signed in with Unity Relay as: " + AuthenticationService.Instance.PlayerId);
        }
        if (isServerBuild)
        {
            CreateRelay();
        }

    }

    void Start()
    {
        hostButton.onClick.AddListener(CreateRelay);
        joinButton.onClick.AddListener(JoinRelay);
    }
    private async void CreateRelay()
    {
        try
        {
            // O número de jogadores é limitado (no plano gratuito)
            var allocation = await RelayService.Instance.CreateAllocationAsync(4);

            // 2. Obtém o "join code" para compartilhar com os clientes
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

            Debug.Log($"Relay Host criado. Código de Conexão: {joinCode}");

            // 3. Converte a alocação do Relay para dados de transporte que o DOTS entende
            relayServerData = allocation.ToRelayServerData("dtls");

            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);

            relayClientData = joinAllocation.ToRelayServerData("dtls");
            if (!isServerBuild)
            {
                StartHost();
            }
            else
            {
                StartServer();
            }

            RelayCode.instance.SetCode(joinCode);

            // LobbyService.Instance.JoinLobbyByCodeAsync(JoinCode);

        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Erro ao criar o Relay Host: {e}");
        }
    }
    private async void JoinRelay()
    {
        try
        {
            JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeTMP.text);

            relayClientData = joinAllocation.ToRelayServerData("dtls");

            StartClient();
            RelayCode.instance.SetCode(joinCodeTMP.text);

        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Erro ao entrar no Relay Join: {e}");
        }
    }
    public async void StartServer()
    {
        // Configura o RelayDriver para o servidor
        var oldContructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData);

        // Cria apenas o mundo de servidor
        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");

        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }

        // Garante que o mundo default seja o servidor
        if (World.DefaultGameObjectInjectionWorld == null)
        {
            World.DefaultGameObjectInjectionWorld = serverWorld;
        }

        // Carrega a cena principal
        await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        // Cria entidade para escutar conexões de clientes
        var networkStreamEntity = serverWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
        serverWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");
        serverWorld.EntityManager.SetComponentData(networkStreamEntity,
            new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

        Debug.Log("Servidor iniciado e aguardando conexões...");
    }

    public async void StartHost()
    {

        var oldContructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");

        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            World.DefaultGameObjectInjectionWorld = serverWorld;
        }

        await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        //localConnection no endereço de loopBack
        // SceneManager.LoadSceneAsync(1, LoadSceneMode.Single);
        // ushort port = 7979;

        // RefRW<NetworkStreamDriver> networkStreamDriver =
        //     serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
        // networkStreamDriver.ValueRW.Listen(NetworkEndpoint.AnyIpv4.WithPort(port));
        // Debug.Log(networkStreamDriver.ValueRO.LastEndPoint.Address);

        // NetworkEndpoint connectNetworkEndpoint = NetworkEndpoint.Parse(joinCodeTMP.text, port);
        // networkStreamDriver =
        //     clientWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamDriver)).GetSingletonRW<NetworkStreamDriver>();
        // networkStreamDriver.ValueRW.Connect(clientWorld.EntityManager, connectNetworkEndpoint);

        var networkStreamEntity = serverWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
        serverWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");
        serverWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

        networkStreamEntity = clientWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        clientWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");

        Debug.Log("relayServerData.Endpoint: " + relayServerData.Endpoint);
        clientWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
    }

    public async void StartClient()
    {
        var oldContructor = NetworkStreamReceiveSystem.DriverConstructor;
        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(new RelayServerData(), relayClientData);
        World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        NetworkStreamReceiveSystem.DriverConstructor = oldContructor;

        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }

        if (World.DefaultGameObjectInjectionWorld == null)
        {
            World.DefaultGameObjectInjectionWorld = clientWorld;
        }

        await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        var networkStreamEntity = clientWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
        clientWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestConnect");
        clientWorld.EntityManager.SetComponentData(networkStreamEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });
    }
}
