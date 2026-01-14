using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Collections;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct RestoreEnergySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();

    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        var simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;

        foreach (var (energyRestore, currentEnergy, MaxEnergy, energyRestoreCooldown, entity) in
                SystemAPI.Query<RefRO<EnergyRestore>, RefRW<CurrentEnergy>, RefRO<MaxEnergy>, DynamicBuffer<EnergyRestoreCooldown>>().WithEntityAccess())
        {
            if (!energyRestoreCooldown.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }

            bool canRestore = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);

            if (!canRestore) return;

            currentEnergy.ValueRW.value += energyRestore.ValueRO.amount;
            if (MaxEnergy.ValueRO.value < currentEnergy.ValueRW.value)
            {
                currentEnergy.ValueRW.value = MaxEnergy.ValueRO.value;
            }
            // Debug.Log("restaurou " + energyRestore.ValueRO.amount);
            //cooldown de ataque
            var newCooldownRestoreEnergy = currentTick;
            newCooldownRestoreEnergy.Add((uint)(energyRestore.ValueRO.cooldownRestore * simulationTickRate));
            energyRestoreCooldown.AddCommandData(new EnergyRestoreCooldown { Tick = currentTick, value = newCooldownRestoreEnergy });
        }

    }
}
