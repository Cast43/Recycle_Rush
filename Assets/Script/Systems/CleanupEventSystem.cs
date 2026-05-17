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
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
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

            bool playerInZone = false;

            // Verifica se algum jogador está dentro do raio (ignorando o eixo Y)
            for (int i = 0; i < players.Length; i++)
            {
                float3 eventPos = transform.ValueRO.Position;
                float3 playerPos = playerTransforms[i].Position;
                
                // Zera o eixo Y para criar um "cilindro" de colisão e evitar bugs de altura
                eventPos.y = 0;
                playerPos.y = 0;

                float distSq = math.distancesq(eventPos, playerPos);
                if (distSq <= radius.ValueRO.value * radius.ValueRO.value)
                {
                    playerInZone = true;
                    break; // Se um estiver dentro, já conta (pode mudar para contar mais rápido se tiver mais gente)
                }
            }

            if (playerInZone)
            {
                // Aumenta o progresso (Tempo)
                objective.ValueRW.Progress += deltaTime;

                // Checa se o objetivo de tempo foi alcançado
                if (objective.ValueRO.Progress >= objective.ValueRO.TargetValue)
                {
                    ecb.AddComponent<EventCompletedTag>(entity);
                    ecb.RemoveComponent<EventActiveTag>(entity);
                    ecb.AddComponent<DestroyEntityTag>(entity); // Destrói o evento ao acabar
                }
            }
        }

        players.Dispose();
        playerTransforms.Dispose();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}