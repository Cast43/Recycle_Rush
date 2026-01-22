using UnityEngine;
using Unity.Entities;
using Unity.NetCode;
using Unity.Networking.Transport.Relay;

// Este é o script que o componente 'Override Automatic Netcode Bootstrap' deve usar.
[UnityEngine.Scripting.Preserve]
public class GameBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979;
        // return base.Initialize(defaultWorldName);
        return false;
    }
}