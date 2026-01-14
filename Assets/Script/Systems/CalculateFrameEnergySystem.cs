using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct CalculateFrameEnergySystem : ISystem
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

        foreach (var (energyBuffer, getEnergyThisTickBuffer, entity) in
                SystemAPI.Query<DynamicBuffer<EnergyBufferElement>, DynamicBuffer<GetEnergyThisTick>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (energyBuffer.IsEmpty)
            {
                getEnergyThisTickBuffer.AddCommandData(new GetEnergyThisTick { Tick = currentTick, value = 0 });
            }
            else
            {
                int totalEnergy = 0;
                if (getEnergyThisTickBuffer.GetDataAtTick(currentTick, out var getEnergyThisTick))
                {
                    totalEnergy = getEnergyThisTick.value;
                }
                foreach (var energy in energyBuffer)
                {
                    totalEnergy += energy.value;
                }
                // Debug.Log("o jogador " + entity + " perdeu " + totalEnergy + " de energia");
                getEnergyThisTickBuffer.AddCommandData(new GetEnergyThisTick { Tick = currentTick, value = totalEnergy });
                energyBuffer.Clear();
            }
        }
    }
}
