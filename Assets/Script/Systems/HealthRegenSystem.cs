using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;
using Unity.Collections;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct HealthRegenSystem : ISystem
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

        foreach (var (healthRegen, currentHealth, maxHealth, healthRegenCooldown, entity) in
                SystemAPI.Query<RefRO<HealthRegen>, RefRW<CurrentHealth>, RefRO<MaxHealth>, DynamicBuffer<HealthRegenCooldown>>().WithAll<GhostOwnerIsLocal>().WithEntityAccess())
        {
            if (currentHealth.ValueRO.value <= 0) return;
            if (!healthRegenCooldown.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }

            bool canRestore = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);

            if (!canRestore) return;
            if (healthRegen.ValueRO.amount <= 0) return;

            currentHealth.ValueRW.value += healthRegen.ValueRO.amount;
            if (maxHealth.ValueRO.value < currentHealth.ValueRW.value)
            {
                currentHealth.ValueRW.value = maxHealth.ValueRO.value;
            }
            Debug.Log("restaurou " + healthRegen.ValueRO.amount);
            //cooldown de ataque
            var newCooldownRestoreEnergy = currentTick;
            newCooldownRestoreEnergy.Add((uint)(healthRegen.ValueRO.cooldownRestore * simulationTickRate));
            healthRegenCooldown.AddCommandData(new HealthRegenCooldown { Tick = currentTick, value = newCooldownRestoreEnergy });
        }

    }
}
