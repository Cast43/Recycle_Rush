using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Mathematics;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))] // Removido o PhysicsSystemGroup já que não usamos mais a física para isso
public partial struct EnemyMeleeSystem : ISystem
{
    private EntityQuery targetQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();

        // Cria uma query para buscar todos os possíveis alvos de dano de uma vez
        targetQuery = SystemAPI.QueryBuilder()
            .WithAll<DamageBufferElement, LocalTransform, Team>()
            .WithNone<DestroyEntityTag>()
            .Build();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();

        // Coleta os dados de todos os possíveis alvos. 
        // [DeallocateOnJobCompletion] no Job vai limpar isso da memória automaticamente após o uso.
        var targetEntities = targetQuery.ToEntityArray(Allocator.TempJob);
        var targetTransforms = targetQuery.ToComponentDataArray<LocalTransform>(Allocator.TempJob);
        var targetTeams = targetQuery.ToComponentDataArray<Team>(Allocator.TempJob);

        var damageOnDistanceJob = new MeleeDamageOnDistanceJob
        {
            currentTick = networkTime.ServerTick,
            targetEntities = targetEntities,
            targetTransforms = targetTransforms,
            targetTeams = targetTeams,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        };

        // Usa o ScheduleParallel para distribuir a checagem de distância em várias threads
        state.Dependency = damageOnDistanceJob.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
[WithNone(typeof(DestroyEntityTag))] // Não ataca se estiver sendo destruído
public partial struct MeleeDamageOnDistanceJob : IJobEntity
{
    [ReadOnly] public NetworkTick currentTick;

    // Arrays contendo todos os possíveis alvos. Serão liberados da memória automaticamente após o job.
    [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Entity> targetEntities;
    [DeallocateOnJobCompletion][ReadOnly] public NativeArray<LocalTransform> targetTransforms;
    [DeallocateOnJobCompletion][ReadOnly] public NativeArray<Team> targetTeams;

    public EntityCommandBuffer.ParallelWriter ECB;

    // O IJobEntity itera automaticamente sobre todos os "Atacantes"
    public void Execute(
        Entity attackerEntity,
        [ChunkIndexInQuery] int chunkIndex,
        ref MeleeAttackProperties meleeProperties,
        ref DynamicBuffer<MeleeAttackCooldown> cooldownBuffer,
        ref DynamicBuffer<AlreadyDamagedEntity> alreadyDamagedBuffer,
        in LocalTransform attackerTransform,
        in Team attackerTeam)
    {
        // 1. CHECAGEM DE COOLDOWN
        if (!cooldownBuffer.GetDataAtTick(currentTick, out var cooldownExpirationTick))
        {
            cooldownExpirationTick.value = NetworkTick.Invalid;
        }

        bool canAttack = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);
        if (!canAttack) return;
        alreadyDamagedBuffer.Clear();

        bool attackedAtLeastOne = false;

        // Calcula o range ao quadrado (é mais rápido calcular distância ao quadrado do que usar math.distance que exige raiz quadrada)
        // OBS: Certifique-se de que 'MeleeAttackProperties' possua um float 'attackRange'
        float rangeSq = meleeProperties.attackRange * meleeProperties.attackRange;

        // 2. ITERAÇÃO SOBRE OS ALVOS
        for (int i = 0; i < targetEntities.Length; i++)
        {
            Entity targetEntity = targetEntities[i];

            // Impede que a entidade ataque a si mesma
            if (targetEntity == attackerEntity) continue;

            // Checagem de Fogo Amigo (ignora se for da mesma facção)
            Team targetTeam = targetTeams[i];
            if (attackerTeam.faction == targetTeam.faction) continue;

            // 3. CHECAGEM DE DISTÂNCIA
            LocalTransform targetTransform = targetTransforms[i];
            float distSq = math.distancesq(attackerTransform.Position, targetTransform.Position);

            if (distSq <= rangeSq)
            {
                // Verifica se já não tomou dano deste ataque específico (descomentei a sua lógica)
                bool alreadyHit = false;
                for (int d = 0; d < alreadyDamagedBuffer.Length; d++)
                {
                    if (alreadyDamagedBuffer[d].value.Equals(targetEntity))
                    {
                        alreadyHit = true;
                        break;
                    }
                }

                if (alreadyHit) continue;

                // 4. APLICAÇÃO DO DANO
                ECB.AppendToBuffer(chunkIndex, targetEntity, new DamageBufferElement { value = meleeProperties.damage });
                ECB.AppendToBuffer(chunkIndex, attackerEntity, new AlreadyDamagedEntity { value = targetEntity });

                attackedAtLeastOne = true;
            }
        }

        // 5. ATUALIZA O COOLDOWN (Se acertou alguém)
        if (attackedAtLeastOne)
        {
            var nextCooldownTick = currentTick;
            nextCooldownTick.Add(meleeProperties.cooldownTickCount);

            // Limpa os hits antigos e aplica o novo cooldown
            alreadyDamagedBuffer.Clear();
            ECB.AppendToBuffer(chunkIndex, attackerEntity, new MeleeAttackCooldown { Tick = currentTick, value = nextCooldownTick });
        }
    }
}