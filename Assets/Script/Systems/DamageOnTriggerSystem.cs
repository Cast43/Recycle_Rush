using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(PhysicsSystemGroup))] // nunca altera essa merda isso faz funcionar
[UpdateAfter(typeof(PhysicsSimulationGroup))]
// [UpdateBefore(typeof(AfterPhysicsSystemGroup))]

public partial struct DamageOnTriggerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var damageOnTriggerJob = new BulletDamageOnTriggerJob
        {
            damageOnTriggerLookup = SystemAPI.GetComponentLookup<DamageOnTrigger>(true),
            teamLookup = SystemAPI.GetComponentLookup<Team>(true),
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            ownerLookup = SystemAPI.GetComponentLookup<Owner>(true),
            destroyLookup = SystemAPI.GetComponentLookup<DestroyEntityTag>(true),
            alreadyDamagedLookup = SystemAPI.GetBufferLookup<AlreadyDamagedEntity>(true),
            damageBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>(true),
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
        };
        SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        state.Dependency = damageOnTriggerJob.Schedule(simulationSingleton, state.Dependency);
        // damageOnTriggerJob.Schedule();
        // state.Dependency.Complete();
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct BulletDamageOnTriggerJob : ITriggerEventsJob
{
    [ReadOnly] public ComponentLookup<DamageOnTrigger> damageOnTriggerLookup;
    [ReadOnly] public ComponentLookup<Team> teamLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
    [ReadOnly] public ComponentLookup<Owner> ownerLookup;
    [ReadOnly] public ComponentLookup<DestroyEntityTag> destroyLookup;
    [ReadOnly] public BufferLookup<AlreadyDamagedEntity> alreadyDamagedLookup;
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

        // dont aply damage multiple times
        var alreadyDamagedBuffer = alreadyDamagedLookup[damageDealingEntity];
        foreach (var alreadyDamagedEntity in alreadyDamagedBuffer)
        {
            // Debug.Log(alreadyDamagedEntity.value);
            if (alreadyDamagedEntity.value.Equals(damageRecievingEntity)) return;
        }
        //ignore friendly fire
        if (teamLookup.TryGetComponent(damageDealingEntity, out var damageDealingTeam) &&
            teamLookup.TryGetComponent(damageRecievingEntity, out var damageReceinvingTeam))
        {
            if (damageDealingTeam.faction == damageReceinvingTeam.faction) return;
        }
        var damageOnTrigger = damageOnTriggerLookup[damageDealingEntity];
        var arrowOwner = ownerLookup[damageDealingEntity];

    //alterar isso porque deve resolver o destroy entity System pq se nao quando qualquer coisa for destruida ela vai tentar 
    //adicionar uma coisa no buffer e vai bugar
        if (!destroyLookup.HasComponent(damageRecievingEntity))
        {
            ECB.AppendToBuffer(damageRecievingEntity, new DamageBufferElement { value = damageOnTrigger.value, owner = arrowOwner.Value });
        }
        if (!destroyLookup.HasComponent(damageDealingEntity))
        {
            ECB.AppendToBuffer(damageDealingEntity, new AlreadyDamagedEntity { value = damageRecievingEntity });
        }
        // Debug.Log(damageDealingEntity);
    }
}


