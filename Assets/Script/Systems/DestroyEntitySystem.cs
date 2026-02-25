using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
partial struct DestroyEntitySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<NetworkTime>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        if (!networkTime.IsFirstPredictionTick) return;
        NetworkTick currentTick = networkTime.ServerTick;

        EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        ComponentLookup<SpawnOnDeath> spawnOnDeathLookup = SystemAPI.GetComponentLookup<SpawnOnDeath>();

        foreach (var (transform, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<DestroyEntityTag, Simulate>().WithEntityAccess())
        {
            // if (state.World.IsServer())
            {
                if (spawnOnDeathLookup.HasComponent(entity))
                {
                    var entitySpawn = ECB.Instantiate(spawnOnDeathLookup[entity].entity);
                    // ECB.SetComponent(entitySpawn, LocalTransform.FromPosition(localTransform.ValueRO.Position - new float3(0, localTransform.ValueRO.Position.y, 0)));
                    var fixPos = LocalTransform.FromPosition(transform.ValueRO.Position - new float3(0, transform.ValueRO.Position.y, 0));
                    ECB.SetComponent(entitySpawn, LocalTransform.FromPositionRotationScale(
                        fixPos.Position,
                        quaternion.identity,
                        spawnOnDeathLookup[entity].scale
                    ));
                }
                ECB.DestroyEntity(entity);
            }
            // else
            // {
            // transform.ValueRW.Position = new float3(1000f, 1000f, 1000f);
            // }

        }
    }
}
