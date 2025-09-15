using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

// [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PhysicsSystemGroup))] // nunca altera essa merda isso faz funcionar
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial struct EffectOnTriggerSystem : ISystem
{
    // [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        var teamLookup = SystemAPI.GetComponentLookup<Team>(true);
        var transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true);
        var ownerLookup = SystemAPI.GetComponentLookup<Owner>(true);
        var destroyLookup = SystemAPI.GetComponentLookup<DestroyEntityTag>(true);
        var alreadyDamagedLookup = SystemAPI.GetBufferLookup<AlreadyDamagedEntity>(true);
        var damageBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>(true);
        var effectBufferLookup = SystemAPI.GetBufferLookup<EffectPrefab>(true);

        var effectOnTriggerJob = new EffectOnTriggerJob
        {
            currentTick = currentTick,
            teamLookup = teamLookup,
            transformLookup = transformLookup,
            ownerLookup = ownerLookup,
            destroyLookup = destroyLookup,
            alreadyDamagedLookup = alreadyDamagedLookup,
            damageBufferLookup = damageBufferLookup,
            effectsBufferLookup = effectBufferLookup,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
        };
        state.Dependency = effectOnTriggerJob.Schedule(simulationSingleton, state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct EffectOnTriggerJob : ITriggerEventsJob
{
    [ReadOnly] public NetworkTick currentTick;
    [ReadOnly] public ComponentLookup<Team> teamLookup;
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
    [ReadOnly] public ComponentLookup<Owner> ownerLookup;
    [ReadOnly] public ComponentLookup<DestroyEntityTag> destroyLookup;
    [ReadOnly] public BufferLookup<AlreadyDamagedEntity> alreadyDamagedLookup;
    [ReadOnly] public BufferLookup<DamageBufferElement> damageBufferLookup;
    [ReadOnly] public BufferLookup<EffectPrefab> effectsBufferLookup;
    public EntityCommandBuffer ECB;
    public void Execute(TriggerEvent triggerEvent)
    {
        Entity damageDealingEntity;
        Entity damageRecievingEntity;
        if (damageBufferLookup.HasBuffer(triggerEvent.EntityA) &&
            effectsBufferLookup.HasBuffer(triggerEvent.EntityB))
        {
            damageRecievingEntity = triggerEvent.EntityA;
            damageDealingEntity = triggerEvent.EntityB;
        }
        else if (effectsBufferLookup.HasBuffer(triggerEvent.EntityA) &&
                damageBufferLookup.HasBuffer(triggerEvent.EntityB))
        {
            damageDealingEntity = triggerEvent.EntityA;
            damageRecievingEntity = triggerEvent.EntityB;
        }
        else return;
        // dont aply damage multiple times
        var alreadyDamagedBuffer = alreadyDamagedLookup[damageDealingEntity];
        foreach (var alreadyDamagedEntity in alreadyDamagedBuffer)
        {
            if (alreadyDamagedEntity.value.Equals(damageRecievingEntity)) return;
        }
        //ignore friendly fire
        if (teamLookup.TryGetComponent(damageDealingEntity, out var damageDealingTeam) &&
            teamLookup.TryGetComponent(damageRecievingEntity, out var damageReceinvingTeam))
        {
            if (damageDealingTeam.faction == damageReceinvingTeam.faction) return;
        }
        var arrowOwner = ownerLookup[damageDealingEntity];
        //alterar isso porque deve resolver o destroy entity System pq se nao quando qualquer coisa for destruida ela vai tentar 
        //adicionar uma coisa no buffer e vai bugar
        //adiciona o novo tempo de envenenamento
        // var newPoisonDuration = currentTick;
        // newPoisonDuration.Add(lightningOnTrigger.duration);
        // var newPoisonDps = currentTick;
        // newPoisonDps.Add(lightningOnTrigger.dmgPerSecond);
        DynamicBuffer<EffectPrefab> effectsBuff = new DynamicBuffer<EffectPrefab>();
        if (effectsBufferLookup.HasBuffer(damageDealingEntity))
        {
            effectsBuff = effectsBufferLookup[damageDealingEntity];
        }

        if (!destroyLookup.HasComponent(damageRecievingEntity))
        {
            // Para cada prefab de efeito configurado, instancia uma cópia para o alvo
            foreach (var prefabElem in effectsBuff)
            {
                var effectInstance = ECB.Instantiate(prefabElem.Prefab);
                // Associa o efeito ao alvo, via um componente “Target” ou via Parent
                ECB.AddComponent(effectInstance, new EffectTarget { Value = damageRecievingEntity, effectGiver = arrowOwner.Value });

            }
        }
    }
}