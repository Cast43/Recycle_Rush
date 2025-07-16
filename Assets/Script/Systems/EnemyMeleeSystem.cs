using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.Mathematics;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PhysicsSystemGroup))] // nunca altera essa merda isso faz funcionar
// [UpdateAfter(typeof())]
public partial struct EnemyMeleeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Limpa todos os buffers de AlreadyDamagedEntity antes de processar as colisões
        // foreach (var buffer in SystemAPI.Query<DynamicBuffer<AlreadyDamagedEntity>>())
        // {
        //     buffer.Clear();
        // }

        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        var damageOnTriggerJob = new MeleeDamageOnTriggerJob
        {
            currentTick = networkTime.ServerTick,
            teamLookup = SystemAPI.GetComponentLookup<Team>(true),
            meleePropretiesLookup = SystemAPI.GetComponentLookup<MeleeAttackProperties>(true),
            destroyLookup = SystemAPI.GetComponentLookup<DestroyEntityTag>(true),
            meleeCooldownLookup = SystemAPI.GetBufferLookup<MeleeAttackCooldown>(true),
            alreadyDamagedLookup = SystemAPI.GetBufferLookup<AlreadyDamagedEntity>(true),
            damageBufferLookup = SystemAPI.GetBufferLookup<DamageBufferElement>(true),
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged),
        };
        SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        state.Dependency = damageOnTriggerJob.Schedule(simulationSingleton, state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct MeleeDamageOnTriggerJob : ICollisionEventsJob
{
    [ReadOnly] public NetworkTick currentTick;
    [ReadOnly] public ComponentLookup<Team> teamLookup;
    [ReadOnly] public ComponentLookup<MeleeAttackProperties> meleePropretiesLookup;
    [ReadOnly] public ComponentLookup<DestroyEntityTag> destroyLookup;
    [ReadOnly] public BufferLookup<MeleeAttackCooldown> meleeCooldownLookup;
    [ReadOnly] public BufferLookup<AlreadyDamagedEntity> alreadyDamagedLookup;
    [ReadOnly] public BufferLookup<DamageBufferElement> damageBufferLookup;
    public EntityCommandBuffer ECB;
    public void Execute(CollisionEvent collisionEvent)
    {
        Entity damageDealingEntity;
        Entity damageRecievingEntity;

        if (damageBufferLookup.HasBuffer(collisionEvent.EntityA) &&
            meleePropretiesLookup.HasComponent(collisionEvent.EntityB))
        {
            damageRecievingEntity = collisionEvent.EntityA;
            damageDealingEntity = collisionEvent.EntityB;
        }
        else if (meleePropretiesLookup.HasComponent(collisionEvent.EntityA) &&
                damageBufferLookup.HasBuffer(collisionEvent.EntityB))
        {
            damageDealingEntity = collisionEvent.EntityA;
            damageRecievingEntity = collisionEvent.EntityB;
        }
        else
        {
            return;
        }

        if (!meleeCooldownLookup.HasBuffer(damageDealingEntity))
            return;

        var meleeCooldownBuffer = meleeCooldownLookup[damageDealingEntity];

        if (!meleeCooldownBuffer.GetDataAtTick(currentTick, out var cooldownExpirationTick))
        {
            cooldownExpirationTick.value = NetworkTick.Invalid;
        }
        bool canAttack = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);
        // Debug.Log(canAttack);

        if (!canAttack) return;

        // dont aply damage multiple times
        var alreadyDamagedBuffer = alreadyDamagedLookup[damageDealingEntity];
        // foreach (var alreadyDamagedEntity in alreadyDamagedBuffer)
        // {
        //     // Debug.Log(alreadyDamagedEntity.value);
        //     if (alreadyDamagedEntity.value.Equals(damageRecievingEntity)) return;
        // }
        //ignore friendly fire
        if (teamLookup.TryGetComponent(damageDealingEntity, out var damageDealingTeam) &&
            teamLookup.TryGetComponent(damageRecievingEntity, out var damageReceinvingTeam))
        {
            if (damageDealingTeam.faction == damageReceinvingTeam.faction) return;
        }
        var damageOnTrigger = meleePropretiesLookup[damageDealingEntity];
        // Debug.Log("ataca");

        //alterar isso porque deve resolver o destroy entity System pq se nao quando qualquer coisa for destruida ela vai tentar 
        //adicionar uma coisa no buffer e vai bugar
        if (!destroyLookup.HasComponent(damageRecievingEntity))
        {
            ECB.AppendToBuffer(damageRecievingEntity, new DamageBufferElement { value = damageOnTrigger.damage });
        }
        if (!destroyLookup.HasComponent(damageDealingEntity))
        {
            ECB.AppendToBuffer(damageDealingEntity, new AlreadyDamagedEntity { value = damageRecievingEntity });
            // Adiciona o cooldown para o próximo ataque
            var nextCooldownTick = currentTick;
            nextCooldownTick.Add(damageOnTrigger.cooldownTickCount);
            ECB.AppendToBuffer(damageDealingEntity, new MeleeAttackCooldown { Tick = currentTick, value = nextCooldownTick });
        }

    }
}
