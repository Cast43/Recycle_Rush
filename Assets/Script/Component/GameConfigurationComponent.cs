using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

public struct RestartGameRpc : IRpcCommand { }
public struct PlayerSpawnedTag : IRpcCommand { }

public struct GameConfig : IComponentData
{
    public Entity playerPrefabA;
    public Entity playerPrefabB;
    public Entity playerPrefabC;
    public Entity playerPrefabD;
    public float timeToNextEnemy;
    public int startWave;
}
