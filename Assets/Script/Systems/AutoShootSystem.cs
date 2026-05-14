using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

[UpdateInGroup(typeof(PausableLateSimulationGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[WithNone(typeof(NeedRessurection))]
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
        if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState) && matchState.IsPaused) return;

        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        // EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        // Entity entitiesReferencesEntity = SystemAPI.GetSingletonEntity<EntitiesReferences>();

        state.Dependency = new ShootJob
        {
            currentTick = networkTime.ServerTick,
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            shootAttackProperties = SystemAPI.GetComponentLookup<ShootAttackProperties>(true),
            currentEnergy = SystemAPI.GetComponentLookup<CurrentEnergy>(true),
            energyBuffer = SystemAPI.GetBufferLookup<EnergyBufferElement>(true),
            effectPrefabsLookup = SystemAPI.GetBufferLookup<EffectPrefab>(true),
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
        [ReadOnly] public ComponentLookup<CurrentEnergy> currentEnergy;
        [ReadOnly] public BufferLookup<EnergyBufferElement> energyBuffer;
        [ReadOnly] public BufferLookup<EffectPrefab> effectPrefabsLookup;
        [ReadOnly] public float deltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        public void Execute(
            ref DynamicBuffer<ShootAttackCooldown> attackCooldown,
            ref DynamicBuffer<DontMoveOnTimer> dontMoveOnTimer,
            ref DynamicBuffer<PendingShootElement> pendingShoots, // NOVO: Buffer de tiros agendados
            in Team unit,
            in ShootAttackProperties attackProperties,
            in TargetEntity target,
            Entity entity,
            [ChunkIndexInQuery] int sortKey)
        {
            if (!transformLookup.HasComponent(target.value)) return;

            float3 spawnPos = transformLookup[entity].Position;
            float3 targetPos = transformLookup[target.value].Position;
            spawnPos = new float3(spawnPos.x, 0, spawnPos.z);
            targetPos = new float3(targetPos.x, 0, targetPos.z);

            var shootProperties = shootAttackProperties[entity];

            // 1. PROCESSAR TIROS PENDENTES (A RAJADA)
            // Lemos de trás para frente para poder remover do buffer em segurança
            for (int i = pendingShoots.Length - 1; i >= 0; i--)
            {
                var pendingShot = pendingShoots[i];

                // Se o tick atual chegou no tick agendado ou já passou dele
                if (!pendingShot.spawnTick.IsNewerThan(currentTick))
                {
                    // Cria a semente única
                    uint seed = (uint)(entity.Index + 1) * (uint)math.abs(currentTick.GetHashCode()) + (uint)(i + 1);
                    if (seed == 0) seed = 1;
                    var random = new Unity.Mathematics.Random(seed);

                    // 1. Gera um deslocamento aleatório para X e Z (se o seu jogo for 2D/TopDown)
                    float radius = shootProperties.spreadRadius; // O quão impreciso é o tiro
                    float randomOffsetX = random.NextFloat(-radius, radius);
                    float randomOffsetZ = random.NextFloat(-radius, radius);

                    // 2. Cria uma "falsa" posição do alvo adicionando essa variação
                    float3 variedTargetPos = targetPos + new float3(randomOffsetX, 0, randomOffsetZ);

                    // 3. Calcula a direção final baseada nesse alvo "impreciso"
                    float3 finalShootDirection = math.normalizesafe(variedTargetPos - spawnPos);

                    // Criar entidade da flecha usando a nova direção
                    Entity arrowEntity = ECB.Instantiate(sortKey, shootProperties.attackPrefab);

                    ECB.SetComponent(sortKey, arrowEntity, LocalTransform.FromPositionRotation(
                        spawnPos + shootProperties.firePointOffset,
                        quaternion.LookRotationSafe(finalShootDirection, math.up())));

                    // Continua com o resto das suas definições...
                    ECB.SetComponent(sortKey, arrowEntity, new Team { faction = unit.faction });
                    ECB.SetComponent(sortKey, arrowEntity, new Direction { lookDirection = finalShootDirection });
                    ECB.SetComponent(sortKey, arrowEntity, new Owner { Value = entity });
                    ECB.SetComponent(sortKey, arrowEntity, new DamageOnTrigger { value = (int)shootProperties.damage });
                    ECB.SetComponent(sortKey, arrowEntity, new DestroyOnTimer { value = shootProperties.bulletLifeTime });
                    ECB.SetComponent(sortKey, arrowEntity, new Arrow { moveSpeed = (int)shootProperties.bulletSpeed });

                    if (effectPrefabsLookup.HasBuffer(entity))
                    {
                        foreach (var item in effectPrefabsLookup[entity])
                        {
                            ECB.AppendToBuffer(sortKey, arrowEntity, item);
                        }
                    }

                    // Remover o tiro que acabou de ser processado do buffer
                    pendingShoots.RemoveAt(i);
                }
            }

            // 2. VERIFICAR SE PODE INICIAR UM NOVO ATAQUE
            if (!attackCooldown.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }

            bool canAttack = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);

            if (!canAttack) return;

            // Verifica energia
            if (energyBuffer.HasBuffer(entity))
            {
                var lostEnergy = shootAttackProperties[entity].lostEnergy + shootProperties.damage;
                if (currentEnergy[entity].value < lostEnergy)
                {
                    return;
                }
                ECB.AppendToBuffer(sortKey, entity, new EnergyBufferElement { value = (int)lostEnergy });
            }

            // 3. AGENDAR A RAJADA DE TIROS
            // Em vez de atirar direto, agendamos os tiros no buffer baseado em 'shotCount'
            int shotsToFire = math.max(1, shootProperties.shotCount); // Garante no mínimo 1 tiro
            for (int i = 0; i < shotsToFire; i++)
            {
                var spawnTick = currentTick;
                spawnTick.Add((uint)(i * shootProperties.ticksBetweenShots)); // Adiciona o delay para cada tiro subsequente

                pendingShoots.Add(new PendingShootElement { spawnTick = spawnTick });
            }

            // Aplicar Cooldowns gerais do ataque
            var newCooldownTickAttack = currentTick;
            newCooldownTickAttack.Add(attackProperties.cooldownTickCount);
            attackCooldown.AddCommandData(new ShootAttackCooldown { Tick = currentTick, value = newCooldownTickAttack });

            var newCooldownTickMove = currentTick;
            newCooldownTickMove.Add(attackProperties.timeToDontMove);
            dontMoveOnTimer.AddCommandData(new DontMoveOnTimer { Tick = currentTick, value = newCooldownTickMove });
        }
    }
}
