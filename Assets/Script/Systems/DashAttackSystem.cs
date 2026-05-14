using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(PausableSimulationGroup))]
partial struct DashAttackSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        var simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;

        state.Dependency = new DashAttack
        {
            currentTick = networkTime.ServerTick,
            targetLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true),
            simulationTickRate = simulationTickRate,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        }.ScheduleParallel(state.Dependency);
    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct DashAttack : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;
        // MUDOU AQUI: Sem tag perigosa, e usando LocalToWorld
        [ReadOnly] public ComponentLookup<LocalToWorld> targetLookup;
        [ReadOnly] public int simulationTickRate;
        public EntityCommandBuffer.ParallelWriter ECB;

        public void Execute(
                    ref DynamicBuffer<DashCooldown> dashCooldown,
                    ref DynamicBuffer<DashDuration> dashDuration,
                    ref DynamicBuffer<DontMoveOnTimer> dontMoveOnTimer,
                    ref LocalTransform transform,
                    ref EnemyDashProperties dashProperties,
                    ref PhysicsVelocity physicsVelocity,
                    in TargetEntity target,
                    in Movement movement,
                    in MoveSpeed speed,
                    Entity entity,
                    [ChunkIndexInQuery] int sortKey)
        {
            // 1. Verifica se o alvo ainda existe e tem o componente (Evita o erro de Entidade Destruída)
            if (!targetLookup.HasComponent(target.value)) return;

            // 2. Calcula posição, offset e a distância (Aqui está a variável que estava faltando!)
            var targetPos = targetLookup[target.value].Position;
            var offset = targetPos - transform.Position;
            float distanceToTarget = math.length(offset);

            // 3. GERENCIA O ESTADO: O inimigo JÁ ESTÁ no meio do dash?
            if (dashProperties.isDashing)
            {
                if (!dashDuration.GetDataAtTick(currentTick, out var dashDurationTick))
                {
                    dashDurationTick.value = NetworkTick.Invalid;
                }

                // Se o tick atual alcançou ou passou do tick de duração, o dash acabou
                if (dashDurationTick.value.IsValid && !dashDurationTick.value.IsNewerThan(currentTick))
                {
                    dashProperties.isDashing = false;
                    physicsVelocity.Linear = float3.zero; // Freia o inimigo ao fim do dash
                }
                else
                {
                    // Ainda está no meio do tempo de voo do Dash. 
                    return;
                }
            }

            // Verifica se o cooldown já expirou
            if (!dashCooldown.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }

            // Pode dar o dash se não houver cooldown salvo OU se o tick atual já passou do tempo
            // 4. GERENCIA O ATAQUE: O inimigo NÃO está dando dash. Ele pode dar um agora?
            bool canDash = !cooldownExpirationTick.value.IsValid || !cooldownExpirationTick.value.IsNewerThan(currentTick);
            if (distanceToTarget > dashProperties.aggroDistance)
            {
                canDash = false;
            }

            if (canDash)
            {

                // INICIA O DASH
                dashProperties.isDashing = true;

                // NORMALIZAÇÃO: Garante que o vetor tenha tamanho 1.
                float3 dashDirection = distanceToTarget > 0.001f ? math.normalize(offset) : float3.zero;

                dashDirection.y = 0;
                // Aplica a física
                physicsVelocity.Angular = float3.zero;
                physicsVelocity.Linear = dashDirection * dashProperties.speed;
                transform.Rotation = quaternion.identity;

                // Calcula os Ticks futuros para Cooldown e Duração
                var expireDurationTick = currentTick;
                expireDurationTick.Add((uint)(dashProperties.duration * simulationTickRate));

                var expireCooldownTick = currentTick;
                expireCooldownTick.Add((uint)(dashProperties.cooldown * simulationTickRate));

                // Registra nos Buffers de comando
                dashDuration.AddCommandData(new DashDuration { Tick = currentTick, value = expireDurationTick });
                dashCooldown.AddCommandData(new DashCooldown { Tick = currentTick, value = expireCooldownTick });

                // Trava o movimento padrão do inimigo durante o dash
                // dontMoveOnTimer.AddCommandData(new DontMoveOnTimer { Tick = currentTick, value = expireDurationTick });
            }
            else
            {
                physicsVelocity.Angular = float3.zero;
                physicsVelocity.Linear = movement.moveVector * speed.currentSpeed;
                transform.Rotation = quaternion.identity;
            }
        }
    }
}
