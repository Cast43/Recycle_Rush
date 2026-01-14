using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial struct EnergyBarSystem : ISystem
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

        foreach (var CurrentHealth
        in SystemAPI.Query<RefRW<CurrentHealth>>().WithAll<GhostOwnerIsLocal>())
        {
            float currentValue = CurrentHealth.ValueRO.value;
            UIManager.Instance.UpdateHealthPercentage(currentValue);
        }
    }
}
