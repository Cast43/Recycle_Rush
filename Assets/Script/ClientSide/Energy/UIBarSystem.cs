using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial struct UIBarSystem : ISystem
{
    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Segurança: Se a UI não carregou ainda, não faz nada
        if (UIManager.Instance == null) return;

        foreach (var currentEnergy
        in SystemAPI.Query<RefRW<CurrentEnergy>>().WithAll<GhostOwnerIsLocal>())
        {
            float currentValue = currentEnergy.ValueRO.value;

            // Envia para o mundo GameObject (Canvas)
            UIManager.Instance.UpdateEnergyPercentage(currentValue);
        }
        if (UIManager.Instance == null) return;

        foreach (var (currentHealth, maxHealth)
        in SystemAPI.Query<RefRW<CurrentHealth>, RefRO<MaxHealth>>().WithAll<GhostOwnerIsLocal>())
        {
            int currentValue = currentHealth.ValueRO.value;
            int maxValue = maxHealth.ValueRO.value;
            UIManager.Instance.UpdateRobotHealthPercentage(currentValue, maxValue);
        }

        foreach (var currentExperience
        in SystemAPI.Query<RefRW<CurrentExperience>>().WithAll<GhostOwnerIsLocal>())
        {
            int currentValue = currentExperience.ValueRO.value;
            UIManager.Instance.UpdateExperienceBar(currentValue);
        }
    }
}
