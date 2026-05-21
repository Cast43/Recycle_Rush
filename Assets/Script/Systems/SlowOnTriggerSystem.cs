using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Physics;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PausableSimulationGroup))]
partial struct SlowOnTriggerSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
        // 1. REQUIRE THE UNMANAGED SINGLETON
        state.RequireForUpdate<ClientServerTickRate>(); 
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.ServerTick;
        
        // 2. THE FIX: USE THE UNMANAGED SINGLETON INSTEAD OF MANAGED CONFIG
        var simulationTickRate = SystemAPI.GetSingleton<ClientServerTickRate>().SimulationTickRate;

        var moveSpeedLookup = state.GetComponentLookup<MoveSpeed>();
        var teamLookup = state.GetComponentLookup<Team>();

        foreach (var (areaSlow, durationAreaSlow, currentTeam, localTransform, alreadyDamagedBuffer, entity)
             in SystemAPI.Query<RefRO<AreaSlow>, RefRW<DurationAreaSlow>, RefRO<Team>, RefRO<LocalTransform>, DynamicBuffer<AlreadyDamagedEntity>>().WithEntityAccess())
        {
            if (!durationAreaSlow.ValueRO.tick.IsValid)
            {
                var initTick = currentTick;
                initTick.Add((uint)(areaSlow.ValueRO.duration * simulationTickRate));
                durationAreaSlow.ValueRW.tick = initTick;
                return;
            }

            bool isExpired = !durationAreaSlow.ValueRO.tick.IsNewerThan(currentTick);
            bool isBeingDestroyed = SystemAPI.HasComponent<DestroyEntityTag>(entity);

            if (isExpired || isBeingDestroyed)
            {
                for (int i = alreadyDamagedBuffer.Length - 1; i >= 0; i--)
                {
                    Entity trackedEntity = alreadyDamagedBuffer[i].value;
                    if (moveSpeedLookup.HasComponent(trackedEntity))
                    {
                        var speed = moveSpeedLookup[trackedEntity];
                        speed.currentSpeed += areaSlow.ValueRO.slowAmount;
                        moveSpeedLookup[trackedEntity] = speed;
                    }
                }
                alreadyDamagedBuffer.Clear(); // Limpa todo mundo de uma vez
                if (!isBeingDestroyed)
                {
                    ECB.AddComponent<DestroyEntityTag>(entity);
                }
                return;
            }

            NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
            CollisionFilter collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = (1u << GameAssets.UNIT_LAYER) | (1u << GameAssets.PLAYER_LAYER),
                GroupIndex = 0
            };

            if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, localTransform.ValueRO.Scale, ref hits, collisionFilter))
            {
                // Criamos um HashSet para busca rápida (O(1)) de quem foi atingido NESTE frame
                NativeHashSet<Entity> currentHits = new NativeHashSet<Entity>(hits.Length, Allocator.Temp);
                foreach (var hit in hits)
                {
                    currentHits.Add(hit.Entity);
                }

                // -------------------------------------------------------------
                // 1. LÓGICA DE SAÍDA (OnTriggerExit)
                // -------------------------------------------------------------
                for (int i = alreadyDamagedBuffer.Length - 1; i >= 0; i--)
                {
                    Entity trackedEntity = alreadyDamagedBuffer[i].value;

                    // Se a entidade que estava no buffer não foi detectada na esfera neste frame...
                    if (!currentHits.Contains(trackedEntity))
                    {
                        // Ela SAIU! Vamos restaurar a velocidade (se a entidade ainda existir)
                        if (moveSpeedLookup.HasComponent(trackedEntity))
                        {
                            var speed = moveSpeedLookup[trackedEntity];
                            speed.currentSpeed += areaSlow.ValueRO.slowAmount; // Devolve o speed
                            moveSpeedLookup[trackedEntity] = speed;
                        }

                        // Removemos do buffer usando SwapBack (mais performático no DOTS que RemoveAt)
                        alreadyDamagedBuffer.RemoveAtSwapBack(i);
                    }
                }

                // -------------------------------------------------------------
                // 2. LÓGICA DE ENTRADA (OnTriggerEnter)
                // -------------------------------------------------------------
                foreach (var hit in hits)
                {
                    bool alreadyHit = false;
                    for (int i = 0; i < alreadyDamagedBuffer.Length; i++)
                    {
                        if (alreadyDamagedBuffer[i].value.Equals(hit.Entity))
                        {
                            alreadyHit = true;
                            break;
                        }
                    }

                    if (alreadyHit) continue;

                    if (teamLookup.HasComponent(hit.Entity) && moveSpeedLookup.HasComponent(hit.Entity))
                    {
                        var hitTeam = teamLookup[hit.Entity];
                        if (hitTeam.faction != currentTeam.ValueRO.faction)
                        {
                            var speed = moveSpeedLookup[hit.Entity];
                            speed.currentSpeed -= areaSlow.ValueRO.slowAmount; // Aplica o slow
                            moveSpeedLookup[hit.Entity] = speed;

                            alreadyDamagedBuffer.Add(new AlreadyDamagedEntity { value = hit.Entity });
                        }
                    }
                }

                currentHits.Dispose();
            }
            else
            {
                // Se a OverlapSphere não pegou NINGUÉM neste frame, mas o buffer tem gente, 
                // significa que todo mundo que estava lá dentro saiu.
                for (int i = alreadyDamagedBuffer.Length - 1; i >= 0; i--)
                {
                    Entity trackedEntity = alreadyDamagedBuffer[i].value;
                    if (moveSpeedLookup.HasComponent(trackedEntity))
                    {
                        var speed = moveSpeedLookup[trackedEntity];
                        speed.currentSpeed += areaSlow.ValueRO.slowAmount;
                        moveSpeedLookup[trackedEntity] = speed;
                    }
                }
                alreadyDamagedBuffer.Clear(); // Limpa todo mundo de uma vez
            }

            hits.Dispose();
        }
    }
}