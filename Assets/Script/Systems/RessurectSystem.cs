using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
// [UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
partial struct RessurectSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        var currentTick = networkTime.ServerTick;
        Entity ressurectedEntity = Entity.Null;

        //verifica se a duração da area de ressuireição acabou
        foreach (var (localTransform, ressurectionDuration, entity) in SystemAPI.Query<RefRO<LocalTransform>, DynamicBuffer<RessurectionDuration>>().WithEntityAccess().WithNone<DestroyEntityTag>())
        {
            if (!ressurectionDuration.GetDataAtTick(currentTick, out var ressurectDurationElement))
            {
                ressurectDurationElement.value = NetworkTick.Invalid;
            }
            bool endRessurectDuration = !ressurectDurationElement.value.IsValid || currentTick.IsNewerThan(ressurectDurationElement.value);

            ressurectedEntity = ressurectDurationElement.ressurectedEntity;

            if (endRessurectDuration)
            {
                ECB.AddComponent<DestroyEntityTag>(ressurectDurationElement.ressurectedEntity);
                ECB.AddComponent<DestroyEntityTag>(entity);
            }
        }
        //verifica se há alguma colisão dentro da área
        foreach (var (localTransform, ressurectProperties, ressurectDuration, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<RessurectProperties>, DynamicBuffer<RessurectionDuration>>().WithEntityAccess().WithNone<DestroyEntityTag>())
        {
            if (!ressurectDuration.GetDataAtTick(currentTick, out var ressurectDurationElement))
            {
                ressurectDurationElement.value = NetworkTick.Invalid;
            }

            NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            CollisionFilter collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.PLAYER_LAYER,
                GroupIndex = 0
                // CollidesWith = (1u << GameAssets.PLAYER_LAYER) | (1u << GameAssets.ENEMY_LAYER),
            };
            // bool InArea = false;
            if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, ressurectProperties.ValueRO.radius, ref hits, collisionFilter))
            {
                foreach (var hit in hits)
                {
                    Entity hitEntity = hit.Entity;
                    if (SystemAPI.HasComponent<CurrentHealth>(hitEntity))
                    {
                        if (SystemAPI.HasComponent<Team>(hitEntity))
                        {
                            //se a própria entidade tenta se reviver 
                            if (ressurectedEntity != hitEntity)
                            {
                                var teamLookup = SystemAPI.GetComponentLookup<Team>();
                                var team = teamLookup[hitEntity];
                                //verifica se são do mesmo time
                                if (team.faction == ressurectProperties.ValueRO.team)
                                {
                                    var ressurectionTimeAreaLookup = SystemAPI.GetBufferLookup<TimeInRessurectionArea>();
                                    //veririca se o aliados já estava na area ressucitando para não adicionar novamente o buffer
                                    if (SystemAPI.HasBuffer<TimeInRessurectionArea>(hitEntity))
                                    {
                                        var ressurectionTimeArea = ressurectionTimeAreaLookup[hitEntity];
                                        //verificação para adicionar o time apenas 1 vez
                                        if (ressurectionTimeArea.IsEmpty)
                                        {
                                            var NewinAreaTick = currentTick;
                                            NewinAreaTick.Add(ressurectProperties.ValueRO.minTimeInArea);
                                            ressurectionTimeArea.AddCommandData(new TimeInRessurectionArea
                                            {
                                                Tick = currentTick,
                                                value = NewinAreaTick,
                                                areaCollider = entity,
                                                ressurectedEntity = ressurectDurationElement.ressurectedEntity,
                                            });
                                        }
                                    }
                                    else
                                    {
                                        //adiciona o buffer para entidade atingida
                                        ECB.AddBuffer<TimeInRessurectionArea>(hitEntity);
                                    }
                                }
                            }
                        }
                    }
                }
            }

            hits.Dispose();

        }
        //procurar pelas entidades TimeInRessurectionArea por um lookup enquanto o foreach olha a entidade com a tag NeedRessurection
        //verifica se o alido ficou tempo suficiente na ressurectZone do outro aliado ressucitado
        foreach (var (localTransform, ressurectionTimeArea, entity) in SystemAPI.Query<RefRO<LocalTransform>, DynamicBuffer<TimeInRessurectionArea>>().WithEntityAccess().WithNone<DestroyEntityTag>())
        {
            if (!ressurectionTimeArea.IsEmpty)
            {
                if (!ressurectionTimeArea.GetDataAtTick(currentTick, out var timeInAreaBufferElement))
                {
                    timeInAreaBufferElement.value = NetworkTick.Invalid;
                }

                bool canRessurect = !timeInAreaBufferElement.value.IsValid || currentTick.IsNewerThan(timeInAreaBufferElement.value);
                if (canRessurect)
                {
                    ECB.AddComponent<ResetLife>(timeInAreaBufferElement.ressurectedEntity);
                    ECB.AddComponent<DestroyEntityTag>(timeInAreaBufferElement.areaCollider);
                    ECB.RemoveComponent<TimeInRessurectionArea>(entity);
                }
                var ressurectPropertiesLookup = SystemAPI.GetComponentLookup<RessurectProperties>();
                var colliderPosLookup = SystemAPI.GetComponentLookup<LocalTransform>();
                var ressurectProperties = ressurectPropertiesLookup[timeInAreaBufferElement.areaCollider];
                var colliderPos = colliderPosLookup[timeInAreaBufferElement.areaCollider];
                //se está muito longe da colisão
                if (math.distance(localTransform.ValueRO.Position, colliderPos.Position) > ressurectProperties.radius)
                {
                    ECB.RemoveComponent<TimeInRessurectionArea>(entity);
                }
            }
        }
        //volta a vida do jogador ao normal
        foreach (var (localTransform, resetLife, currentHealth, maxHealth, entity) in SystemAPI.Query<RefRO<LocalTransform>, RefRO<ResetLife>, RefRW<CurrentHealth>, RefRO<MaxHealth>>().WithEntityAccess().WithNone<DestroyEntityTag>())
        {
            ECB.SetComponent(entity, new CurrentHealth { value = maxHealth.ValueRO.value, onHealthChanged = false });
            ECB.RemoveComponent<ResetLife>(entity);
            ECB.RemoveComponent<NeedRessurection>(entity);
        }
    }
}


