using Unity.NetCode;
using Unity.Entities;
using Unity.Networking.Transport.Relay;

/// <summary>
/// Register client and server using Relay server settings.
/// For the client, if the Relay settings are not set and the modality is `Client/Server`, it will
/// try to setup the driver using IPCNetworkInterface.
/// </summary>
public class RelayDriverConstructor : INetworkStreamDriverConstructor
{
    RelayServerData m_RelayClientData;
    RelayServerData m_RelayServerData;

    public RelayDriverConstructor(RelayServerData serverData, RelayServerData clientData)
    {
        m_RelayServerData = serverData;
        m_RelayClientData = clientData;
    }

    /// <summary>
    /// This method will ensure that we register different driver types based on the Relay settings
    /// settings.
    /// <para>
    /// Mode          |  Relay Settings
    /// Client/Server |  Valid -> use Relay to connect to local server
    ///                  Invalid -> use IPC to connect to local server
    /// Client        |  Always use Relay. Expect data to be valid, or exceptions are thrown by Transport.
    /// <para>
    /// <para>
    /// For WebGL, WebSocket is always preferred for client in the Editor, to closely emulate the player behaviour.
    /// </para>
    /// </summary>
    public void CreateClientDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
    {
        var settings = DefaultDriverBuilder.GetNetworkSettings();
        settings.WithRelayParameters(ref m_RelayClientData);
        DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, settings);
        //if the Relay data is not valid, connect via local IPC
        // if (ClientServerBootstrap.RequestedPlayType == ClientServerBootstrap.PlayType.ClientAndServer &&
        //    !m_RelayClientData.Endpoint.IsValid)
        // {
        // DefaultDriverBuilder.RegisterClientIpcDriver(world, ref driverStore, netDebug, settings);
        // }
        // else
        // {
        // #if !UNITY_WEBGL
        //         DefaultDriverBuilder.RegisterClientUdpDriver(world, ref driverStore, netDebug, settings);
        // #else
        //             DefaultDriverBuilder.RegisterClientWebSocketDriver(world, ref driverStore, netDebug, settings);
        // #endif
        // }
    }

    public void CreateServerDriver(World world, ref NetworkDriverStore driverStore, NetDebug netDebug)
    {
        //The first driver is the IPC for internal client/server connection if necessary.
        // IPC can't use Relay and needs to be set up without Relay data.
        // var ipcSettings = DefaultDriverBuilder.GetNetworkServerSettings();
        // DefaultDriverBuilder.RegisterServerIpcDriver(world, ref driverStore, netDebug, ipcSettings);
        // var relaySettings = DefaultDriverBuilder.GetNetworkServerSettings();
        //The other driver (still the same port) is going to listen using Relay for external connections.
        // relaySettings.WithRelayParameters(ref m_RelayServerData);
#if !UNITY_WEBGL || UNITY_EDITOR
        DefaultDriverBuilder.RegisterServerDriver(world, ref driverStore, netDebug, ref m_RelayServerData);
#else
        // DefaultDriverBuilder.RegisterServerWebSocketDriver(world, ref driverStore, netDebug, relaySettings);
#endif
    }
}
