using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Physics;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateBefore(typeof(CalculateFrameExperienceSystem))]
partial struct RestoreEnergySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<ClientServerTickRate>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.ServerTick;

        // Forma segura e oficial de pegar o TickRate
        var simulationTickRate = SystemAPI.GetSingleton<ClientServerTickRate>().SimulationTickRate;

        // 1. LÓGICA DE REGENERAÇÃO PASSIVA
        foreach (var (energyRestore, currentEnergy, maxEnergy, energyRestoreCooldown) in
                 SystemAPI.Query<RefRO<EnergyRestore>, RefRW<CurrentEnergy>, RefRO<MaxEnergy>, RefRW<EnergyRestoreCooldown>>().WithAll<Simulate>())
        {
            // Se a energia já está cheia, empurramos o cooldown para o futuro.
            // Isso previne o "heal instantâneo" ao gastar a primeira barra de energia.
            if (currentEnergy.ValueRO.value >= maxEnergy.ValueRO.value)
            {
                var resetTick = currentTick;
                resetTick.Add((uint)(energyRestore.ValueRO.cooldownRestore * simulationTickRate));
                energyRestoreCooldown.ValueRW.value = resetTick;
                continue;
            }

            // Se for a primeira vez rodando (Tick Inválido), apenas inicia o timer.
            if (!energyRestoreCooldown.ValueRO.value.IsValid)
            {
                var initTick = currentTick;
                initTick.Add((uint)(energyRestore.ValueRO.cooldownRestore * simulationTickRate));
                energyRestoreCooldown.ValueRW.value = initTick;
                continue;
            }

            // Verifica se o tick salvo já passou
            if (currentTick.IsNewerThan(energyRestoreCooldown.ValueRO.value))
            {
                // Restaura a energia e trava no valor máximo
                currentEnergy.ValueRW.value = math.min(currentEnergy.ValueRW.value + energyRestore.ValueRO.amount, maxEnergy.ValueRO.value);

                // Calcula o Tick futuro para a próxima regeneração
                var nextCooldownTick = currentTick;
                nextCooldownTick.Add((uint)(energyRestore.ValueRO.cooldownRestore * simulationTickRate));

                // Salva o estado atualizado
                energyRestoreCooldown.ValueRW.value = nextCooldownTick;
            }
        }

        // 2. LÓGICA DE REGENERAÇÃO POR MOVIMENTO
        foreach (var (currentEnergy, maxEnergy, energyMovement, physicsVelocity) in
                 SystemAPI.Query<RefRW<CurrentEnergy>, RefRO<MaxEnergy>, RefRW<EnergyRestoreMovement>, RefRO<PhysicsVelocity>>().WithAll<Simulate>())
        {
            if (currentEnergy.ValueRO.value >= maxEnergy.ValueRO.value) continue;

            if (energyMovement.ValueRO.distance >= energyMovement.ValueRO.maxDistance)
            {
                currentEnergy.ValueRW.value = math.min(currentEnergy.ValueRW.value + energyMovement.ValueRO.amount, maxEnergy.ValueRO.value);
                energyMovement.ValueRW.distance = 0;
            }
            else
            {
                energyMovement.ValueRW.distance += math.length(physicsVelocity.ValueRO.Linear);
            }
        }

        // 3. LÓGICA DE REGENERAÇÃO POR ABATE (KILLS)
        foreach (var (currentEnergy, maxEnergy, energyRestoreKill, killBuffer) in
                 SystemAPI.Query<RefRW<CurrentEnergy>, RefRO<MaxEnergy>, RefRO<EnergyRestoreKill>, DynamicBuffer<GetEnergyFromKill>>().WithAll<Simulate>())
        {
            if (killBuffer.IsEmpty) continue;

            foreach (var kill in killBuffer)
            {
                if (currentEnergy.ValueRO.value >= maxEnergy.ValueRO.value) break;
                currentEnergy.ValueRW.value = math.min(currentEnergy.ValueRW.value + energyRestoreKill.ValueRO.amount, maxEnergy.ValueRO.value);
            }
            killBuffer.Clear();
        }
    }
}