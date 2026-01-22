using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

partial struct InicializateDestroyOnTimerSystem : ISystem
{
    // [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ECB = new EntityCommandBuffer(Allocator.Temp);
        var simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
        var currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

        foreach (var (destroyOnTimer, entity) in SystemAPI.Query<DestroyOnTimer>().WithNone<DestroyAtTick>().WithEntityAccess())
        {
            var lifetimeInTicks = (uint)(destroyOnTimer.value * simulationTickRate);
            var targetTick = currentTick;
            targetTick.Add(lifetimeInTicks);

            ECB.AddComponent(entity, new DestroyAtTick { value = targetTick });
        }
        ECB.Playback(state.EntityManager);
    }
}
