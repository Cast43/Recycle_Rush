using Unity.NetCode;
using UnityEngine;


//automaticamente bootstrap o jogo. não precisa existir um game objecto para funcionar
[UnityEngine.Scripting.Preserve]
public class GameBootstrap : ClientServerBootstrap
{
    public override bool Initialize(string defaultWorldName)
    {
        AutoConnectPort = 7979;
        return base.Initialize(defaultWorldName);
    }
}
