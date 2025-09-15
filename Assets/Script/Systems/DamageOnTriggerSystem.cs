using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

// [UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateInGroup(typeof(PhysicsSystemGroup))] // nunca altera essa merda isso faz funcionar
[UpdateAfter(typeof(PhysicsSimulationGroup))]

public partial struct DamageOnTriggerSystem : ISystem
{
    // [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<SimulationSingleton>();
        // state.RequireForUpdate<EndFixedStepSimulationEntityCommandBufferSystem.Singleton>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        //existe durante o frame atual e guarda colisões no mesmo frame
        // var alreadyDamagedThisFrame = new NativeHashSet<Entity>(16, Allocator.TempJob);

        var damageOnTriggerJob = new BulletDamageOnTriggerJob
        {
            damageOnTriggerLookup = SystemAPI.GetComponentLookup<DamageOnTrigger>(true),
            teamLookup = SystemAPI.GetComponentLookup<Team>(true),
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            ownerLookup = SystemAPI.GetComponentLookup<Owner>(true),
            destroyLookup = SystemAPI.GetComponentLookup<DestroyEntityTag>(true),
            alreadyDamagedLookup = SystemAPI.GetBufferLookup<AlreadyDamagedEntity>(true),
            damageBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>(true),
            currentTick = currentTick,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
        };
        state.Dependency = damageOnTriggerJob.Schedule(simulationSingleton, state.Dependency);
        state.Dependency.Complete(); // Espera job acabar antes de aplicar ECB (para segurança)

        // ECB.Playback(state.EntityManager);
        // ECB.Dispose();
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
    [ReadOnly] public NetworkTick currentTick;
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

        var alreadyDamagedBuffer = alreadyDamagedLookup[damageDealingEntity];
        foreach (var alreadyDamagedEntity in alreadyDamagedBuffer)
        {
            // Debug.Log(alreadyDamagedEntity.value);
            if (alreadyDamagedEntity.value.Equals(damageRecievingEntity)) return;
            // Debug.Log(alreadyDamagedEntity.value);
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
        // if (!destroyLookup.HasComponent(damageRecievingEntity))
        // dont aply damage multiple times

        ECB.AppendToBuffer(damageDealingEntity, new AlreadyDamagedEntity { value = damageRecievingEntity });
        ECB.AppendToBuffer(damageRecievingEntity, new DamageBufferElement { value = damageOnTrigger.value, owner = arrowOwner.Value });
    }
}


