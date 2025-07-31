using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

[UpdateInGroup(typeof(LateSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct AutoShootSystem : ISystem
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

        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        Entity entitiesReferencesEntity = SystemAPI.GetSingletonEntity<EntitiesReferences>();

        state.Dependency = new ShootJob
        {
            currentTick = networkTime.ServerTick,
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            shootAttackProperties = SystemAPI.GetComponentLookup<ShootAttackProperties>(true),
            deltaTime = SystemAPI.Time.DeltaTime,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
        }.ScheduleParallel(state.Dependency);


    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct ShootJob : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;
        [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
        [ReadOnly] public ComponentLookup<ShootAttackProperties> shootAttackProperties;
        [ReadOnly] public float deltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        public void Execute(ref DynamicBuffer<ShootAttackCooldown> attackCooldown, ref DynamicBuffer<DontMoveOnTimer> dontMoveOnTimer, in Team unit, in ShootAttackProperties attackProperties, in TargetEntity target, Entity entity, [ChunkIndexInQuery] int sortKey)
        {
            if (!transformLookup.HasComponent(target.value)) return;
            if (!attackCooldown.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }

            bool canAttack = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);

            if (!canAttack) return;

            float3 spawnPos = transformLookup[entity].Position + attackProperties.firePointOffset;
            float3 targetPos = transformLookup[target.value].Position;

            // Criar entidade da flecha
            // Direção da flecha baseada no olhar do jogador
            Entity arrowEntity = ECB.Instantiate(sortKey, shootAttackProperties[entity].attackPrefab);
            // float3 targetDir = transformLookup[target.value].Position;
            // float3 localPos = transformLookup[entity].Position;
            float3 shootDirection = math.normalizesafe(targetPos - spawnPos);

            ECB.SetComponent(sortKey, arrowEntity, LocalTransform.FromPositionRotation(spawnPos,
                quaternion.LookRotationSafe(targetPos - spawnPos, math.up())));
            // Apenas modificar a direção sem sobrescrever os outros valores
            ECB.SetComponent(sortKey, arrowEntity, new Team { faction = unit.faction }); //modifica apenas uma variável sem sobreescrever tudo
            ECB.SetComponent(sortKey, arrowEntity, new Direction { lookDirection = shootDirection }); //modifica apenas uma variável sem sobreescrever tudo
            ECB.SetComponent(sortKey, arrowEntity, new Owner { Value = entity }); //modifica apenas uma variável sem sobreescrever tudo

            //cooldown de ataque
            var newCooldownTickAttack = currentTick;
            newCooldownTickAttack.Add(attackProperties.cooldownTickCount);
            attackCooldown.AddCommandData(new ShootAttackCooldown { Tick = currentTick, value = newCooldownTickAttack });

            //cooldown de movimento
            var newCooldownTickMove = currentTick;
            newCooldownTickMove.Add(attackProperties.timeToDontMove);
            dontMoveOnTimer.AddCommandData(new DontMoveOnTimer { Tick = currentTick, value = newCooldownTickMove });

        }
    }

}
