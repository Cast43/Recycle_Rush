using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

// 1. OBRIGATÓRIO: Apenas o servidor deve mandar a mensagem de dano global
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct CalculateFrameDamageSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

        // Criamos o Buffer de Comandos
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);

        // 2. CORREÇÃO DA QUERY: Pegamos todas as conexões ANTES do loop principal
        // Isso evita o erro de "Nested SystemAPI.Query"
        EntityQuery connectionQuery = SystemAPI.QueryBuilder().WithAll<NetworkId, NetworkStreamInGame>().Build();
        NativeArray<Entity> activeConnections = connectionQuery.ToEntityArray(Allocator.Temp);

        foreach (var (damageBuffer, damageThisTickBuffer, localTransform, entity) in
                 SystemAPI.Query<DynamicBuffer<DamageBufferElement>, DynamicBuffer<DamageThisTick>, RefRO<LocalTransform>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (damageBuffer.IsEmpty)
            {
                damageThisTickBuffer.AddCommandData(new DamageThisTick { Tick = currentTick, value = 0, owner = Entity.Null });
            }
            else
            {
                int totalDamage = 0;
                Entity GetXP = Entity.Null;

                if (damageThisTickBuffer.GetDataAtTick(currentTick, out var damageThisTick))
                {
                    totalDamage = damageThisTick.value;
                }

                foreach (var damage in damageBuffer)
                {
                    if (damage.value > 0)
                    {
                        // 3. Iteramos sobre o NativeArray que já buscamos lá em cima
                        foreach (Entity connectionEntity in activeConnections)
                        {
                            Entity rpcEntity = ECB.CreateEntity();

                            ECB.AddComponent(rpcEntity, new DamageNumberRpc
                            {
                                Position = localTransform.ValueRO.Position,
                                DamageAmount = damage.value
                            });

                            ECB.AddComponent(rpcEntity, new SendRpcCommandRequest
                            {
                                TargetConnection = connectionEntity
                            });
                        }
                    }

                    totalDamage += damage.value;
                    GetXP = damage.owner;
                }

                damageThisTickBuffer.AddCommandData(new DamageThisTick { Tick = currentTick, value = totalDamage, owner = GetXP });
                damageBuffer.Clear();
            }
        }

        // 4. CORREÇÃO CRÍTICA: Executar e limpar a memória!
        // Sem isso, os RPCs nunca seriam criados de verdade.
        ECB.Playback(state.EntityManager);
        ECB.Dispose();

        // Limpamos o array temporário de conexões
        activeConnections.Dispose();
    }
}