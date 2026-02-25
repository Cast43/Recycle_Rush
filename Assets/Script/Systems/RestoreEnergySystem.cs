using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;
using Unity.Collections;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
//gerador de biomassa precisa disso
[UpdateBefore(typeof(CalculateFrameExperienceSystem))]

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

        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (energyRestore, currentEnergy, MaxEnergy, energyRestoreCooldown, entity) in
                SystemAPI.Query<RefRO<EnergyRestore>, RefRW<CurrentEnergy>, RefRO<MaxEnergy>, DynamicBuffer<EnergyRestoreCooldown>>().WithEntityAccess())
        {
            if (currentEnergy.ValueRW.value >= MaxEnergy.ValueRO.value) continue;

            if (energyRestoreCooldown.IsEmpty)
            {
                var newCooldownRestoreEnergy = currentTick;
                newCooldownRestoreEnergy.Add((uint)(energyRestore.ValueRO.cooldownRestore * simulationTickRate));
                energyRestoreCooldown.AddCommandData(new EnergyRestoreCooldown { Tick = currentTick, value = newCooldownRestoreEnergy });
            }

            if (!energyRestoreCooldown.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }

            bool canRestore = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);

            if (!canRestore) continue;

            currentEnergy.ValueRW.value += energyRestore.ValueRO.amount;
            if (MaxEnergy.ValueRO.value < currentEnergy.ValueRW.value)
            {
                currentEnergy.ValueRW.value = MaxEnergy.ValueRO.value;
            }
            energyRestoreCooldown.Clear();
            // Debug.Log("restaurou " + energyRestore.ValueRO.amount);
            //cooldown de ataque
        }

        foreach (var (currentEnergy, maxEnergy, energyMovement, physicsVelocity, entity) in
        SystemAPI.Query<RefRW<CurrentEnergy>, RefRO<MaxEnergy>,
        RefRW<EnergyRestoreMovement>, RefRO<PhysicsVelocity>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (currentEnergy.ValueRW.value >= maxEnergy.ValueRO.value) continue;
            if (energyMovement.ValueRO.distance >= energyMovement.ValueRO.maxDistance)
            {
                currentEnergy.ValueRW.value += energyMovement.ValueRO.amount;
                energyMovement.ValueRW.distance = 0;
            }
            else
            {
                energyMovement.ValueRW.distance += math.length(physicsVelocity.ValueRO.Linear);
            }
        }

        foreach (var (currentEnergy, maxEnergy, energyRestoreKill, killBuffer, entity) in
        SystemAPI.Query<RefRW<CurrentEnergy>, RefRO<MaxEnergy>,
        RefRW<EnergyRestoreKill>, DynamicBuffer<GetEnergyFromKill>>().WithAll<Simulate>().WithEntityAccess())
        {
            foreach (var kill in killBuffer)
            {
                if (currentEnergy.ValueRW.value >= maxEnergy.ValueRO.value) continue;
                // currentEnergy.ValueRW.value += energyRestoreKill.ValueRO.amount;
                currentEnergy.ValueRW.value = math.min(currentEnergy.ValueRW.value + energyRestoreKill.ValueRO.amount, maxEnergy.ValueRO.value);
            }
            killBuffer.Clear();
        }
    }
}
