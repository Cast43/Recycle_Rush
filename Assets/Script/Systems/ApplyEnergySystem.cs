using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameEnergySystem))]

partial struct ApplyEnergySystem : ISystem
{
    // [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();

    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);

        //remove energia para cada valor no buffer de energia usado por tiro etc
        foreach (var (currentEnergy, maxEnergy, getEnergyThisTickBuffer, entity) in
            SystemAPI.Query<RefRW<CurrentEnergy>, RefRO<MaxEnergy>
            , DynamicBuffer<GetEnergyThisTick>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (!getEnergyThisTickBuffer.GetDataAtTick(currentTick, out var getEnergyThisTick)) continue;
            if (getEnergyThisTick.Tick != currentTick) continue;

            if (currentEnergy.ValueRO.value >= getEnergyThisTick.value)
            {
                currentEnergy.ValueRW.value -= getEnergyThisTick.value;
            }
        }
    }
}
