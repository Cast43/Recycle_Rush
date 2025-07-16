using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
partial struct DestroyEntitySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        if (!networkTime.IsFirstPredictionTick) return;
        NetworkTick currentTick = networkTime.ServerTick;

        EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (transform, entity) in SystemAPI.Query<RefRW<LocalTransform>>().WithAll<DestroyEntityTag, Simulate>().WithEntityAccess())
        {
            if (state.World.IsServer())
            {
                ECB.DestroyEntity(entity);
            }
            else
            {
                transform.ValueRW.Position = new float3(1000f, 1000f, 1000f);
            }

        }
    }
}
