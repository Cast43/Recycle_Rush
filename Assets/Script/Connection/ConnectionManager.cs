using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Unity.NetCode;
using Unity.Networking.Transport;
using Unity.Collections;
using Unity.Entities;
using TMPro;
public class ConnectionManager : MonoBehaviour
{
    [SerializeField] private TMP_InputField addressField;
    [SerializeField] private Button joinButton;
    [SerializeField] private Button hostButton;

    public string address => addressField.text;

    void Start()
    {
        hostButton.onClick.AddListener(StartHost);
        joinButton.onClick.AddListener(StartClient);
    }

    void Update()
    {

    }
    public void StartServer()
    {
        DestroyLocalSimulationWorld();
        SceneManager.LoadScene(1);
        var serverWorld = ClientServerBootstrap.CreateServerWorld("Server World");

        var serverEndPoint = NetworkEndpoint.AnyIpv4.WithPort(7777);
        {
            using var networkDriverQuerry = serverWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuerry.GetSingletonRW<NetworkStreamDriver>().ValueRW.Listen(serverEndPoint);
        }
        Debug.Log(serverEndPoint.Address);
    }
    public void StartHost()
    {
        StartServer();
        StartClient();
    }
    public void StartClient()
    {
        DestroyLocalSimulationWorld();
        SceneManager.LoadScene(1);

        var clientWorld = ClientServerBootstrap.CreateClientWorld("Client World");

        var connectionEndPoint = NetworkEndpoint.Parse(address, 7777);
        {
            using var networkDriverQuerry = clientWorld.EntityManager.CreateEntityQuery(ComponentType.ReadWrite<NetworkStreamDriver>());
            networkDriverQuerry.GetSingletonRW<NetworkStreamDriver>().ValueRW.Connect(clientWorld.EntityManager, connectionEndPoint);
        }
        World.DefaultGameObjectInjectionWorld = clientWorld;
    }

    private static void DestroyLocalSimulationWorld()
    {
        foreach (var world in World.All)
        {
            if (world.Flags == WorldFlags.Game)
            {
                world.Dispose();
                break;
            }
        }
    }

}
