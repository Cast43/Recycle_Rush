using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Physics;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PausableSimulationGroup))]
public partial struct DamageAreaSystem : ISystem
{
    private ComponentLookup<CurrentHealth> healthLookup;
    private ComponentLookup<Team> teamLookup;

    public void OnCreate(ref SystemState state)
    {
        healthLookup = state.GetComponentLookup<CurrentHealth>();
        teamLookup = state.GetComponentLookup<Team>();
    }

    public void OnUpdate(ref SystemState state)
    {
        healthLookup.Update(ref state);
        teamLookup.Update(ref state);

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var simulationTickRate = NetCodeConfig.Global.ClientServerTickRate.SimulationTickRate;
        var currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        var ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // Note que trocamos os Buffers pelo AreaDamageTimer (RefRW para podermos alterar os tempos)
        foreach (var (areaDamage, timer, targetTeam, localTransform, visualState, alreadyDamagedBuffer, entity)
                 in SystemAPI.Query<RefRO<AreaDamage>, RefRW<AreaDamageTimer>, RefRO<Team>, RefRO<LocalTransform>, RefRW<AreaVisualState>, DynamicBuffer<AlreadyDamagedEntity>>().WithEntityAccess())
        {
            // Se o timer nunca foi inicializado, ajustamos para a primeira preparação
            if (!timer.ValueRO.NextPhaseTick.IsValid)
            {
                var initTick = currentTick;
                initTick.Add((uint)(areaDamage.ValueRO.timeToDmg * simulationTickRate));
                timer.ValueRW.NextPhaseTick = initTick;
                timer.ValueRW.CurrentPhase = AreaPhase.Preparing;
            }

            // === FASE 1: PREPARAÇÃO ===
            if (timer.ValueRO.CurrentPhase == AreaPhase.Preparing)
            {
                alreadyDamagedBuffer.Clear();
                visualState.ValueRW.IsImpacting = false; // Cliente mostra o visual de "Start"

                // Se o tempo de preparação acabou, mudamos para a fase de IMPACTO
                if (currentTick.IsNewerThan(timer.ValueRO.NextPhaseTick))
                {
                    timer.ValueRW.CurrentPhase = AreaPhase.Impacting;

                    // Calcula quando o impacto vai acabar (dmgInterval serve como duração da fase de dano)
                    var nextTick = currentTick;
                    nextTick.Add((uint)(areaDamage.ValueRO.dmgInterval * simulationTickRate));
                    timer.ValueRW.NextPhaseTick = nextTick;

                    // Opcional: Se quiser que ele cause dano MÚLTIPLAS VEZES durante o impacto, inicializa o NextDamageTick aqui.
                    // Para esse exemplo, vamos causar o dano a cada tick da simulação enquanto estiver na fase de Impacto.
                }
            }
            // === FASE 2: IMPACTO (DANO) ===
            else if (timer.ValueRO.CurrentPhase == AreaPhase.Impacting)
            {
                visualState.ValueRW.IsImpacting = true; // Cliente mostra o visual de "End" (explosão/área)

                // Lógica de Dano (Aplicada enquanto estiver nesta fase)
                NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
                CollisionFilter collisionFilter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = (1u << GameAssets.UNIT_LAYER) | (1u << GameAssets.PLAYER_LAYER),
                    GroupIndex = 0
                };

                if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, areaDamage.ValueRO.radius, ref hits, collisionFilter))
                {
                    foreach (var hit in hits)
                    {
                        // 1. Verificamos se a entidade já está no buffer
                        bool alreadyHit = false;
                        for (int i = 0; i < alreadyDamagedBuffer.Length; i++)
                        {
                            if (alreadyDamagedBuffer[i].value.Equals(hit.Entity))
                            {
                                alreadyHit = true;
                                break; // Para o loop do buffer, já achamos!
                            }
                        }

                        // 2. Se já tomou hit, pula para o PRÓXIMO HIT (usando continue, não return)
                        if (alreadyHit) continue;

                        // 3. Aplica o dano se for da facção certa
                        if (teamLookup.HasComponent(hit.Entity) && healthLookup.HasComponent(hit.Entity))
                        {
                            var team = teamLookup[hit.Entity];
                            if (team.faction == targetTeam.ValueRO.faction)
                            {
                                ECB.AppendToBuffer(hit.Entity, new DamageBufferElement { value = areaDamage.ValueRO.dmgPerTick });


                                // OTIMIZAÇÃO: Adiciona direto no buffer em vez de usar ECB
                                alreadyDamagedBuffer.Add(new AlreadyDamagedEntity { value = hit.Entity });
                            }
                        }
                    }
                }
                hits.Dispose();

                // Agora o código vai chegar aqui sem ser abortado pelo "return"!
                if (currentTick.IsNewerThan(timer.ValueRO.NextPhaseTick))
                {
                    timer.ValueRW.CurrentPhase = AreaPhase.Preparing;

                    var nextTick = currentTick;
                    nextTick.Add((uint)(areaDamage.ValueRO.timeToDmg * simulationTickRate));
                    timer.ValueRW.NextPhaseTick = nextTick;
                }
            }
        }
    }
}