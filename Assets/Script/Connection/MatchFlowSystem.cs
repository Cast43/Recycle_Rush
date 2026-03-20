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
                matchStateRW.ValueRW.CurrentState = MatchState.Tutorial;
            }
        }
        else if (currentState == MatchState.Tutorial || currentState == MatchState.Playing)
        {
            if (waitingCount > 0) SpawnPlayers(ref state);
        }

        // NOVA LÓGICA: Verifica se o Tutorial acabou
        if (currentState == MatchState.Tutorial)
        {
            int totalPlayers = 0;
            int completedPlayers = 0;

            // Passa por todos os jogadores que estão no tutorial
            foreach (var progress in SystemAPI.Query<RefRO<TutorialProgress>>())
            {
                totalPlayers++;

                // Conta quantos já têm a flag IsCompleted como true
                if (progress.ValueRO.IsCompleted)
                {
                    completedPlayers++;
                }
            }

            // Se existe alguém na sala e a quantidade de pessoas que completaram 
            // é igual ao total de pessoas jogando...
            if (totalPlayers > 0 && completedPlayers == totalPlayers)
            {
                UnityEngine.Debug.Log("Todos completaram o Tutorial! Mudando para o Jogo Real.");

                // Muda o estado! A HUD do tutorial vai sumir para todos os clientes.
                matchStateRW.ValueRW.CurrentState = MatchState.Playing;
            }
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

            // ecb.AddComponent(playerAvatar, new TutorialProgress { CurrentStep = 0, IsCompleted = false });

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