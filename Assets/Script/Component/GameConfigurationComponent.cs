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
}

[GhostComponent]
public struct MatchStateComponent : IComponentData
{
    [GhostField] public MatchState CurrentState;
}
public struct WaitingToSpawnTag : IComponentData { }

// O RPC que o cliente envia para o servidor
public struct PlayerReadyRpc : IRpcCommand { }

// A Tag que o servidor coloca na conexão do jogador para saber que ele está pronto
public struct PlayerReadyTag : IComponentData { }

[GhostComponent]
public struct LobbyStateComponent : IComponentData
{
    [GhostField] public int ConnectedPlayers;
    [GhostField] public int ReadyPlayers;
}
[GhostComponent]
public struct TutorialProgress : IComponentData
{
    // O passo atual da missão (0, 1, 2, 3...)
    [GhostField] public int CurrentStep;

    // Se o jogador já terminou todo o tutorial
    [GhostField] public bool IsCompleted;
}