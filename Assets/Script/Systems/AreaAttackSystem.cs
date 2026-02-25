using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using Unity.Collections;

[UpdateInGroup(typeof(SimulationSystemGroup))]
partial struct AreaAttackSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        state.RequireForUpdate<BeginSimulationEntityCommandBufferSystem.Singleton>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        var simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;


        state.Dependency = new AreaAttack
        {
            currentTick = networkTime.ServerTick,
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
            deltaTime = SystemAPI.Time.DeltaTime,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            simulationTickRate = simulationTickRate,
        }.ScheduleParallel(state.Dependency);

    }

    [BurstCompile]
    [WithAll(typeof(Simulate))]
    public partial struct AreaAttack : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;
        [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
        [ReadOnly] public float deltaTime;
        [ReadOnly] public int simulationTickRate;
        public EntityCommandBuffer.ParallelWriter ECB;

        [BurstCompile]
        public void Execute(ref CooldownAreaAttack attackCooldown,
                    ref DynamicBuffer<DontMoveOnTimer> dontMoveOnTimer,
                    in LocalTransform transform,
                    in AreaAttackProperties props,
                    in TargetEntity target,
                    Entity entity,
                    [ChunkIndexInQuery] int sortKey)
        {
            // 1. INICIALIZAÇÃO
            // Se não for válido (quando o componente acabou de ser criado), inicializamos para o futuro
            if (!attackCooldown.NextPhaseTick.IsValid)
            {
                var initTick = currentTick;
                initTick.Add((uint)(props.TimeToAttack * simulationTickRate));
                attackCooldown.NextPhaseTick = initTick;
                return;
            }

            // 2. BLOQUEIO DE COOLDOWN
            // Lê-se: "Se o tick do próximo ataque é MAIS NOVO (está no futuro) que o tick atual..."
            if (attackCooldown.NextPhaseTick.IsNewerThan(currentTick))
            {
                return; // ...então o cooldown ainda não acabou. Sai do Job.
            }

            // 3. CHECAGEM DE DISTÂNCIA
            // Se o código chegou aqui, o cooldown já acabou (currentTick passou do NextPhaseTick)
            var targetPos = transformLookup[target.value];
            float shootDistance = math.distance(targetPos.Position, transform.Position);

            if (shootDistance > props.aggroDistance)
            {
                return; // Alvo muito longe. O cooldown fica "pronto" esperando ele chegar perto.
            }

            // 4. ATAQUE (Instanciar o objeto de vidro reciclável)
            Entity areaAttackEntity = ECB.Instantiate(sortKey, props.attack);

            ECB.SetComponent(sortKey, areaAttackEntity, new AreaDamage
            {
                dmgPerTick = props.dmgPerTick,
                timeToDmg = props.timeToDmg,
                dmgInterval = props.dmgInterval,
                radius = props.radiusArea,
            });

            ECB.SetComponent(sortKey, areaAttackEntity, new DestroyOnTimer { value = props.areaDuration });

            targetPos.Position.y = 0;
            ECB.SetComponent(sortKey, areaAttackEntity, LocalTransform.FromPositionRotationScale(
                targetPos.Position,
                quaternion.identity,
                props.radiusArea
            ));

            // 5. ATUALIZAR O PRÓXIMO TICK (Resetar o Cooldown)
            var nextAttackTick = currentTick;
            nextAttackTick.Add((uint)(props.TimeToAttack * simulationTickRate));
            attackCooldown.NextPhaseTick = nextAttackTick;

            // 6. COOLDOWN DE MOVIMENTO
            var newCooldownTickMove = currentTick;
            newCooldownTickMove.Add((uint)(props.timeToDontMove * simulationTickRate));
            dontMoveOnTimer.AddCommandData(new DontMoveOnTimer { Tick = currentTick, value = newCooldownTickMove });
        }
    }


}
