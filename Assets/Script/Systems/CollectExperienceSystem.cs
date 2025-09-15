using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(PhysicsSystemGroup))] // nunca altera essa merda isso faz funcionar
[UpdateAfter(typeof(PhysicsSimulationGroup))]
// [UpdateBefore(typeof(AfterPhysicsSystemGroup))]

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
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);


        foreach (var (getExperience, localTransform, entity) in SystemAPI.Query<RefRW<GetExperienceInArea>, RefRO<LocalTransform>>().WithEntityAccess())
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

            if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, getExperience.ValueRO.radius, ref hits, collisionFilter))
            {
                foreach (var hit in hits)
                {
                    var areadyExperiencedLookup = SystemAPI.GetBufferLookup<AlreadyGiveExperienceEntity>();
                    var GiverExperienceComponentLookup = SystemAPI.GetComponentLookup<GiveExperience>();
                    var alreadyExperiencedBuffer = areadyExperiencedLookup[entity];
                    var giverExperience = GiverExperienceComponentLookup[hit.Entity];
                    foreach (var alreadyExperiencedEntity in alreadyExperiencedBuffer)
                    {
                        // Debug.Log(alreadyDamagedEntity.value);
                        if (alreadyExperiencedEntity.value.Equals(hit.Entity)) return;
                    }

                    ECB.AppendToBuffer(entity, new AlreadyGiveExperienceEntity { value = hit.Entity });
                    ECB.AppendToBuffer(entity, new ExperienceBufferElement { value = giverExperience.value });

                    ECB.AddComponent<DestroyEntityTag>(hit.Entity);
                    // alreadyExperiencedBuffer.Clear();
                }
            }
        }
    }
}

