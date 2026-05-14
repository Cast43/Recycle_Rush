using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct MatchFlowSystem : ISystem
{
    private int playersSpawnedCount;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        playersSpawnedCount = 0;
        state.RequireForUpdate<MatchStateComponent>();
        state.RequireForUpdate<EntitiesReferences>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var matchStateRW = SystemAPI.GetSingletonRW<MatchStateComponent>();
        var lobbyStateRW = SystemAPI.GetSingletonRW<LobbyStateComponent>(); // <- Pegamos o componente de Lobby

        var currentState = matchStateRW.ValueRO.CurrentState;

        var waitingPlayersQuery = SystemAPI.QueryBuilder().WithAll<WaitingToSpawnTag, NetworkId>().Build();
        var readyPlayersQuery = SystemAPI.QueryBuilder().WithAll<WaitingToSpawnTag, PlayerReadyTag, NetworkId>().Build();

        int waitingCount = waitingPlayersQuery.CalculateEntityCount();
        int readyCount = readyPlayersQuery.CalculateEntityCount();

        // ATUALIZA OS VALORES PARA A REDE SINCRONIZAR
        lobbyStateRW.ValueRW.ConnectedPlayers = waitingCount;
        lobbyStateRW.ValueRW.ReadyPlayers = readyCount;

        if (currentState == MatchState.WaitingForPlayers)
        {
            if (waitingCount > 0 && readyCount == waitingCount)
            {
                matchStateRW.ValueRW.CurrentState = MatchState.Playing;
            }
        }
        else if (currentState == MatchState.Playing)
        {
            if (waitingCount > 0) SpawnPlayers(ref state);
        }
    }

    private void SpawnPlayers(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);
        var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        // Procura por todas as conexões que ainda precisam de um avatar
        foreach (var (networkId, entity) in SystemAPI.Query<RefRO<NetworkId>>().WithAll<WaitingToSpawnTag>().WithEntityAccess())
        {
            Entity playerPrefab = Entity.Null;

            // Sua lógica de seleção de prefabs
            if (playersSpawnedCount == 0) playerPrefab = entitiesReferences.playerRPrefab;
            else if (playersSpawnedCount == 1) playerPrefab = entitiesReferences.playerBPrefab;
            else if (playersSpawnedCount == 2) playerPrefab = entitiesReferences.playerYPrefab;
            else playerPrefab = entitiesReferences.playerGPrefab;

            Entity playerAvatar = ecb.Instantiate(playerPrefab);

            // Posição de Spawn (Você pode ter um array de posições baseadas no ID do jogador depois)
            ecb.SetComponent(playerAvatar, LocalTransform.FromPosition(new float3(UnityEngine.Random.Range(-10, +10), 1, 0)));

            // Atribui a posse ao cliente correto para que ele possa enviar inputs
            ecb.AddComponent(playerAvatar, new GhostOwner { NetworkId = networkId.ValueRO.Value });

            // Link bidirecional (Avatar -> Conexão) opcional, mas útil
            ecb.AddComponent(playerAvatar, new ConnectionEntity { Value = entity });

            // Se a conexão cair, o Avatar é destruído junto
            ecb.AppendToBuffer(entity, new LinkedEntityGroup { Value = playerAvatar });

            // REMOVE a tag para não spawnar de novo no próximo frame!
            ecb.RemoveComponent<WaitingToSpawnTag>(entity);

            playersSpawnedCount++;
        }

        ecb.Playback(state.EntityManager);
    }
}