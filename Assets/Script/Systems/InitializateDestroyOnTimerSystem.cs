using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
partial struct InitializateDestroyOnTimerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);
        int simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

        foreach (var (destroyOnTimer, entity) in SystemAPI.Query<DestroyOnTimer>().WithNone<DestroyAtTick>().WithEntityAccess())
        {
            uint lifeTimeInTicks = (uint)(destroyOnTimer.value * simulationTickRate);
            NetworkTick targetTick = currentTick;
            targetTick.Add(lifeTimeInTicks);
            ECB.AddComponent(entity, new DestroyAtTick { value = targetTick });
        }
        ECB.Playback(state.EntityManager);
    }

}
