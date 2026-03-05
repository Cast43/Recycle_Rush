using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
partial struct HealthRegenSystem : ISystem
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

        var simulationTickRate = SystemAPI.GetSingleton<ClientServerTickRate>().SimulationTickRate;

        foreach (var (healthRegen, currentHealth, maxHealth, healthRegenCooldown) in
                 SystemAPI.Query<RefRO<HealthRegen>, RefRW<CurrentHealth>, RefRO<MaxHealth>, RefRW<HealthRegenCooldown>>().WithAll<Simulate>())
        {
            // Se o robô morreu, não faz nada
            if (currentHealth.ValueRO.value <= 0) continue;

            // Se a vida está cheia ou não possui cura no status, empurra o cooldown para o futuro
            if (currentHealth.ValueRO.value >= maxHealth.ValueRO.value || healthRegen.ValueRO.amount <= 0)
            {
                var resetTick = currentTick;
                resetTick.Add((uint)(healthRegen.ValueRO.cooldownRestore * simulationTickRate));
                healthRegenCooldown.ValueRW.value = resetTick;
                continue;
            }

            // Inicializa o timer se acabou de começar a contar (Tick Inválido)
            if (!healthRegenCooldown.ValueRO.value.IsValid)
            {
                var initTick = currentTick;
                initTick.Add((uint)(healthRegen.ValueRO.cooldownRestore * simulationTickRate));
                healthRegenCooldown.ValueRW.value = initTick;
                continue;
            }

            // Se o cooldown terminou, cura!
            if (currentTick.IsNewerThan(healthRegenCooldown.ValueRO.value))
            {
                currentHealth.ValueRW.value = math.min(currentHealth.ValueRW.value + healthRegen.ValueRO.amount, maxHealth.ValueRO.value);

                var nextCooldownTick = currentTick;
                nextCooldownTick.Add((uint)(healthRegen.ValueRO.cooldownRestore * simulationTickRate));

                healthRegenCooldown.ValueRW.value = nextCooldownTick;
            }
        }
    }
}