using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(PhysicsSystemGroup))] // nunca altera essa merda isso faz funcionar
// [UpdateAfter(typeof(CalculateFrameDamageSystem))]
partial struct DestroyOnTriggerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

        var destroyOnTriggerJob = new DestroyOnTriggerJob
        {
            damageOnTriggerLookup = SystemAPI.GetComponentLookup<DamageOnTrigger>(true),
            unitLookup = SystemAPI.GetComponentLookup<Team>(true),
            damageBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>(true),
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
        };
        SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        state.Dependency = destroyOnTriggerJob.Schedule(simulationSingleton, state.Dependency);
    }

}
[BurstCompile]
[WithAll(typeof(Simulate))]
public struct DestroyOnTriggerJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<DamageOnTrigger> damageOnTriggerLookup;
    [ReadOnly] public ComponentLookup<Team> unitLookup;
    [ReadOnly] public BufferLookup<DamageBufferElement> damageBufferLookup;
    public EntityCommandBuffer ECB;
    public void Execute(TriggerEvent triggerEvent)
    {
        Entity damageDealingEntity;
        Entity damageRecievingEntity;

        if (damageBufferLookup.HasBuffer(triggerEvent.EntityA) &&
            damageOnTriggerLookup.HasComponent(triggerEvent.EntityB))
        {
            damageRecievingEntity = triggerEvent.EntityA;
            damageDealingEntity = triggerEvent.EntityB;
        }
        else if (damageOnTriggerLookup.HasComponent(triggerEvent.EntityA) &&
                damageBufferLookup.HasBuffer(triggerEvent.EntityB))
        {
            damageDealingEntity = triggerEvent.EntityA;
            damageRecievingEntity = triggerEvent.EntityB;
        }
        else
        {
            return;
        }
        //ignore friendly fire
        if (unitLookup.TryGetComponent(damageDealingEntity, out var damageDealingTeam) &&
            unitLookup.TryGetComponent(damageRecievingEntity, out var damageReceinvingTeam))
        {
            if (damageDealingTeam.faction == damageReceinvingTeam.faction) return;
        }
        ECB.AddComponent<DestroyEntityTag>(damageDealingEntity);
        // Debug.Log("tag added");


    }
}
