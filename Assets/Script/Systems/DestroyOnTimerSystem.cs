using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct DestroyOnTimerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

        foreach (var (destroyAtTick, entity) in SystemAPI.Query<DestroyAtTick>().WithAll<Simulate>().WithNone<DestroyEntityTag>().WithEntityAccess())
        {
            if (currentTick.Equals(destroyAtTick.value) || currentTick.IsNewerThan(destroyAtTick.value))
            {
                ECB.AddComponent<DestroyEntityTag>(entity);
            }
        }
    }

}
