using Unity.NetCode;
using UnityEngine;


//automaticamente bootstrap o jogo. não precisa existir um game objecto para funcionar
[UnityEngine.Scripting.Preserve]
public class GameBootstrap : ClientServerBootstrap
{
    //esse script da o auto start bom para testes
    public override bool Initialize(string defaultWorldName)
    {
        // AutoConnectPort = 7979;
        // return base.Initialize(defaultWorldName);
        return false;
    }
}
