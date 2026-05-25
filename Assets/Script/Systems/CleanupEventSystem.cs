using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct CleanupEventSystem : ISystem
{
    private NativeHashMap<Entity, float3> previousPositions;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        previousPositions = new NativeHashMap<Entity, float3>(8, Allocator.Persistent);
    }

    [BurstCompile]
    public void OnDestroy(ref SystemState state)
    {
        if (previousPositions.IsCreated)
            previousPositions.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var ms) && ms.IsPaused) return;

        float deltaTime = SystemAPI.Time.DeltaTime;
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // Coleta todos os jogadores para verificar a distância deles até a zona
        var playerQuery = SystemAPI.QueryBuilder().WithAll<PlayerInput, LocalTransform>().Build();
        var players = playerQuery.ToEntityArray(Allocator.Temp);
        var playerTransforms = playerQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);

        var connectionLookup = SystemAPI.GetComponentLookup<ConnectionEntity>(true);
        var networkIdLookup = SystemAPI.GetComponentLookup<NetworkId>(true);

        // Procura eventos Ativos e que não estejam Concluídos
        foreach (var (objective, transform, radius, entity) in 
                 SystemAPI.Query<RefRW<EventObjective>, RefRO<LocalTransform>, RefRO<EventAreaRadius>>()
                 .WithAll<EventActiveTag>()
                 .WithNone<EventCompletedTag>()
                 .WithEntityAccess())
        {
            // Só processamos eventos do tipo Limpeza (Cleanup) ou Rei do Pedaço
            if (objective.ValueRO.Type != EventType.Cleanup && objective.ValueRO.Type != EventType.KingOfTheHill) 
                continue;

            // Decresce o tempo restante se houver algum limite configurado
            if (objective.ValueRO.TimeLimit > 0)
            {
                objective.ValueRW.TimeRemaining -= deltaTime;
                if (objective.ValueRO.TimeRemaining <= 0)
                {
                    ecb.RemoveComponent<EventActiveTag>(entity);
                    ecb.AddComponent<DestroyEntityTag>(entity); // O evento expirou, destrói e ignora recompensa
                    continue;
                }
            }

            float distanceWalkedInZone = 0f;
            bool playerInZone = false;

            // Verifica se algum jogador está dentro do raio (ignorando o eixo Y)
            for (int i = 0; i < players.Length; i++)
            {
                Entity playerEntity = players[i];
                float3 eventPos = transform.ValueRO.Position;
                float3 playerPos = playerTransforms[i].Position;
                
                // Zera o eixo Y para criar um "cilindro" de colisão e evitar bugs de altura
                eventPos.y = 0;
                playerPos.y = 0;

                float distSq = math.distancesq(eventPos, playerPos);
                if (distSq <= radius.ValueRO.value * radius.ValueRO.value)
                {
                    playerInZone = true;
                    
                    // Pega a distância caminhada pelo jogador neste exato frame
                    if (previousPositions.TryGetValue(playerEntity, out float3 prevPos))
                    {
                        float movedDist = math.distance(playerTransforms[i].Position, prevPos);
                        distanceWalkedInZone += movedDist;
                    }
                }
            }

            float progressToAdd = 0f;
            
            // Se for do tipo Limpeza (Cleanup), o progresso é a distância caminhada
            if (objective.ValueRO.Type == EventType.Cleanup)
            {
                progressToAdd = distanceWalkedInZone;
            }
            // Se for Rei do Pedaço (King of the Hill), o progresso é tempo (deltaTime)
            else if (objective.ValueRO.Type == EventType.KingOfTheHill && playerInZone)
            {
                progressToAdd = deltaTime;
            }

            if (progressToAdd > 0)
            {
                // Aumenta o progresso
                objective.ValueRW.Progress += progressToAdd;

                // Checa se o objetivo de tempo foi alcançado
                if (objective.ValueRO.Progress >= objective.ValueRO.TargetValue)
                {
                    ecb.AddComponent<EventCompletedTag>(entity);
                    ecb.RemoveComponent<EventActiveTag>(entity);
                    ecb.AddComponent<DestroyEntityTag>(entity); // Destrói o evento ao acabar

                    // Recompensa do Evento: Envia a melhoria de Energia (Core) para todos os jogadores!
                    for (int i = 0; i < players.Length; i++)
                    {
                        Entity playerEntity = players[i];
                        ecb.AppendToBuffer(playerEntity, new UpgradesPending { upgradeLevel = UpgradeAperance.Event });

                        if (connectionLookup.HasComponent(playerEntity))
                        {
                            var connEntity = connectionLookup[playerEntity].Value;
                            if (networkIdLookup.HasComponent(connEntity))
                            {
                                var netId = networkIdLookup[connEntity].Value;
                                var rpcEntity = ecb.CreateEntity();
                                ecb.AddComponent(rpcEntity, new ShowUpgradesRPC { ClientNetId = netId, upgradeLevel = UpgradeAperance.Event });
                                ecb.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = connEntity });
                            }
                        }
                    }
                }
            }
        }

        // Atualiza a posição atual dos jogadores para usar no próximo frame
        for (int i = 0; i < players.Length; i++)
        {
            previousPositions[players[i]] = playerTransforms[i].Position;
        }

        players.Dispose();
        playerTransforms.Dispose();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}