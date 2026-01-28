using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;

public class ConnectionManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField joinCodeTMP;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button hostButton;
    [SerializeField] private bool isServerBuild = false;

    private static RelayServerData relayServerData;
    private static RelayServerData relayClientData;

    private bool isBusy = false;

    async void Awake()
    {
        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            Debug.Log($"Logado no Relay: {AuthenticationService.Instance.PlayerId}");
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

    private async Task DestroyLocalWorlds()
    {
        var worldsToDispose = new List<World>();
        foreach (var world in World.All)
        {
            if (world != null && world.IsCreated)
            {
                if (world.Flags == WorldFlags.Game || world.Name == "ServerWorld" || world.Name == "ClientWorld")
                {
                    worldsToDispose.Add(world);
                }
            }
        }

        foreach (var world in worldsToDispose)
        {
            if (world != null && world.IsCreated) world.Dispose();
        }

        // Mantemos o delay para garantir que a porta foi liberada
        await Task.Delay(10);
    }

    private async void CreateRelay()
    {
        if (isBusy) return;
        isBusy = true;

        try
        {
            await DestroyLocalWorlds();

            var allocation = await RelayService.Instance.CreateAllocationAsync(4);
            string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Relay Criado. Código: {joinCode}");

            relayServerData = allocation.ToRelayServerData("dtls");
            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
            relayClientData = joinAllocation.ToRelayServerData("dtls");

            RelayCode.instance.SetCode(joinCode);

            if (!isServerBuild) await StartHost();
            else await StartServer();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Erro Relay Host: {e}");
        }
        finally
        {
            isBusy = false;
        }
    }

    private async void JoinRelay()
    {
        if (isBusy) return;
        isBusy = true;

        try
        {
            await DestroyLocalWorlds();

            var joinAllocation = await RelayService.Instance.JoinAllocationAsync(joinCodeTMP.text);
            relayClientData = joinAllocation.ToRelayServerData("dtls");

            RelayCode.instance.SetCode(joinCodeTMP.text);
            await StartClient();
        }
        catch (RelayServiceException e)
        {
            Debug.LogError($"Erro Relay Join: {e}");
        }
        finally
        {
            isBusy = false;
        }
    }

    public async Task StartServer()
    {
        await DestroyLocalWorlds();

        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData);
        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        World.DefaultGameObjectInjectionWorld = serverWorld;

        await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        var query = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamRequestListen));
        if (query.IsEmptyIgnoreFilter)
        {
            var networkStreamEntity = serverWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
            serverWorld.EntityManager.SetName(networkStreamEntity, "NetworkStreamRequestListen");

            // CORREÇÃO AQUI: O Server escuta em AnyIpv4 (Local), o RelayDriver redireciona pra nuvem.
            serverWorld.EntityManager.SetComponentData(networkStreamEntity,
                new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });

            Debug.Log("Servidor Relay Iniciado (AnyIpv4).");
        }
    }

    public async Task StartHost()
    {
        await DestroyLocalWorlds();

        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(relayServerData, relayClientData);

        World serverWorld = ClientServerBootstrap.CreateServerWorld("ServerWorld");
        World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        World.DefaultGameObjectInjectionWorld = serverWorld;

        await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        // --- SERVER ---
        var queryListen = serverWorld.EntityManager.CreateEntityQuery(typeof(NetworkStreamRequestListen));
        if (queryListen.IsEmptyIgnoreFilter)
        {
            var listenEntity = serverWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestListen>());
            serverWorld.EntityManager.SetName(listenEntity, "ListenRequest");

            // CORREÇÃO AQUI: Host Server escuta em AnyIpv4
            serverWorld.EntityManager.SetComponentData(listenEntity, new NetworkStreamRequestListen { Endpoint = NetworkEndpoint.AnyIpv4 });
        }

        // --- CLIENT ---
        // O Cliente continua precisando do Endpoint ESPECÍFICO do Relay para saber onde conectar
        var queryConnect = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamRequestConnect>());

        if (queryConnect.IsEmptyIgnoreFilter)
        {
            var connectEntity = clientWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
            clientWorld.EntityManager.SetName(connectEntity, "ConnectRequest");

            // AQUI MANTÉM: Cliente conecta no endereço do Relay
            clientWorld.EntityManager.SetComponentData(connectEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });

            Debug.Log("Host Relay Conectado.");
        }
    }

    public async Task StartClient()
    {
        await DestroyLocalWorlds();

        NetworkStreamReceiveSystem.DriverConstructor = new RelayDriverConstructor(new RelayServerData(), relayClientData);

        World clientWorld = ClientServerBootstrap.CreateClientWorld("ClientWorld");
        World.DefaultGameObjectInjectionWorld = clientWorld;

        await SceneManager.LoadSceneAsync("SampleScene", LoadSceneMode.Single);

        var queryConnect = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadOnly<NetworkStreamRequestConnect>());

        if (queryConnect.IsEmptyIgnoreFilter)
        {
            var connectEntity = clientWorld.EntityManager.CreateEntity(ComponentType.ReadWrite<NetworkStreamRequestConnect>());
            clientWorld.EntityManager.SetName(connectEntity, "ConnectRequest");

            // Cliente conecta no endereço do Relay
            clientWorld.EntityManager.SetComponentData(connectEntity, new NetworkStreamRequestConnect { Endpoint = relayClientData.Endpoint });

            Debug.Log("Cliente Conectado.");
        }
    }
}