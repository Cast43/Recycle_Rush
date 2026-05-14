using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PausablePhysicsGroup))]

public partial struct CollectExperienceSystem : ISystem
{
    // [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EndSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);


        foreach (var (getExperience, GarbageInventory, localTransform, entity) in SystemAPI.Query<RefRW<GetExperienceInArea>, RefRW<GarbageInventory>, RefRO<LocalTransform>>().WithEntityAccess())
        {

            NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
            // var hits = new NativeList<ColliderHit>(Allocator.Temp);
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
            //cria uma esfera para achar entidades no entorno
            CollisionFilter collisionFilter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.EXPERIENCE_LAYER,
                GroupIndex = 0
                // CollidesWith = (1u << GameAssets.PLAYER_LAYER) | (1u << GameAssets.ENEMY_LAYER),
            };
            // var hitsList = new NativeList<DistanceHit>(Allocator.Temp);

            if (GarbageInventory.ValueRO.GarbageCount >= GarbageInventory.ValueRO.MaxCapacityPerType)
                return;

            if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, getExperience.ValueRO.radius, ref hits, collisionFilter))
            {
                var GiverExperienceComponentLookup = SystemAPI.GetComponentLookup<GiveExperience>();
                var GetCoreComponentLookup = SystemAPI.GetComponentLookup<GetCore>();
                var areadyExperiencedLookup = SystemAPI.GetBufferLookup<AlreadyGiveExperienceEntity>();
                var GiverExperienceBufferLookup = SystemAPI.GetBufferLookup<GetEnergyFromKill>();

                foreach (var hit in hits)
                {
                    if (areadyExperiencedLookup.HasBuffer(entity))
                    {

                        var alreadyExperiencedBuffer = areadyExperiencedLookup[entity];


                        bool alreadyGiven = false;
                        foreach (var alreadyExperiencedEntity in alreadyExperiencedBuffer)
                        {
                            if (alreadyExperiencedEntity.value.Equals(hit.Entity))
                            {
                                alreadyGiven = true;
                                break;
                            }
                        }

                        if (alreadyGiven)
                            continue; // <-- agora sim ignora todo esse hit

                        if (GetCoreComponentLookup.HasComponent(hit.Entity))
                        {
                            // ECB.AppendToBuffer(entity, new CoreUpgradeCount { });
                            ECB.AppendToBuffer<UpgradesPending>(entity, new UpgradesPending
                            {
                                upgradeLevel = UpgradeLevel.Core
                            });
                        }
                        ECB.AppendToBuffer(entity, new AlreadyGiveExperienceEntity { value = hit.Entity });
                    }

                    var amountTrash = GiverExperienceComponentLookup[hit.Entity].value;
                    switch (GiverExperienceComponentLookup[hit.Entity].tarshType)
                    {
                        case TrashType.Plastic:
                            GarbageInventory.ValueRW.PlasticCount += amountTrash;
                            break;
                        case TrashType.Paper:
                            GarbageInventory.ValueRW.PaperCount += amountTrash;
                            break;
                        case TrashType.Glass:
                            GarbageInventory.ValueRW.GlassCount += amountTrash;
                            break;
                        case TrashType.Iron:
                            GarbageInventory.ValueRW.MetalCount += amountTrash;
                            break;
                        case TrashType.Organic:
                            GarbageInventory.ValueRW.OrganicCount += amountTrash;
                            break;
                        case TrashType.NotRecycle:
                            GarbageInventory.ValueRW.NotRecycleCount += amountTrash;
                            break;
                    }
                    GarbageInventory.ValueRW.GarbageCount += amountTrash;

                    // if (GiverExperienceComponentLookup.HasComponent(hit.Entity))
                    // {
                    //     var giverExperience = GiverExperienceComponentLookup[hit.Entity];
                    //     ECB.AppendToBuffer(entity, new ExperienceBufferElement { value = giverExperience.value });
                    // }

                    ECB.AddComponent<DestroyEntityTag>(hit.Entity);
                    if (GiverExperienceBufferLookup.HasBuffer(entity))
                    {
                        ECB.AppendToBuffer(entity, new GetEnergyFromKill { amount = GiverExperienceComponentLookup[hit.Entity].value });
                    }
                }
            }
        }
    }
}
