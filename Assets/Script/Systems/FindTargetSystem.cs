using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PausablePhysicsGroup))]
[UpdateBefore(typeof(ExportPhysicsWorld))]
[WithNone(typeof(NeedRessurection))]
partial struct FindTargetSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
        // BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        // var ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // foreach ((RefRO<LocalTransform> localTransform, RefRO<TargetRadius> radius, RefRW<TargetEntity> target, RefRO<TargetFind> targetFind, Entity entity) in
        //         SystemAPI.Query<RefRO<LocalTransform>, RefRO<TargetRadius>, RefRW<TargetEntity>, RefRO<TargetFind>>().WithEntityAccess())
        // {
        //     // Debug.Log("a entidade " + entity);
        //     hits.Clear();
        CollisionFilter collisionFilter = new CollisionFilter
        {
            BelongsTo = ~0u,
            CollidesWith = (1u << GameAssets.UNIT_LAYER) | (1u << GameAssets.PLAYER_LAYER),
            GroupIndex = 0
            // CollidesWith = (1u << GameAssets.PLAYER_LAYER) | (1u << GameAssets.ENEMY_LAYER),
        };
        //     if (collisionWorld.OverlapSphere(localTransform.ValueRO.Position, radius.ValueRO.value, ref hits, CollisionFilter.Default))
        //     {
        //         foreach (DistanceHit hit in hits)
        //         {

        //             if (!SystemAPI.HasComponent<Team>(hit.Entity)) continue;

        //             Faction targetFaction = SystemAPI.GetComponent<Team>(hit.Entity).faction;
        //             if (targetFind.ValueRO.value == targetFaction)
        //             {
        //                 Debug.Log(hit.Entity);
        //                 // ECB.SetComponent(entity, new TargetEntity { value = hit.Entity });
        //                 target.ValueRW.value = hit.Entity;
        //             }
        //             else
        //             {
        //                 // ECB.SetComponent(entity, new TargetEntity { value = Entity.Null });
        //                 target.ValueRW.value = Entity.Null;
        //             }
        //         }
        //     }
        // }
        var teamLookup = SystemAPI.GetComponentLookup<Team>(true);

        state.Dependency = new FindJob
        {
            collisionWorld = collisionWorld,
            collisionFilter = collisionFilter,
            teamLookup = teamLookup,
            currentHealthLookup = SystemAPI.GetComponentLookup<CurrentHealth>(),

        }
        .ScheduleParallel(state.Dependency);
    }

}
[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct FindJob : IJobEntity
{
    // [ReadOnly] public float deltaTime;
    [ReadOnly] public CollisionWorld collisionWorld;
    [ReadOnly] public CollisionFilter collisionFilter;
    [ReadOnly] public ComponentLookup<Team> teamLookup;
    [ReadOnly] public ComponentLookup<CurrentHealth> currentHealthLookup;

    [BurstCompile]
    public void Execute(in LocalTransform localTransform, in TargetRadius targetRadius, ref TargetEntity target, in TargetFind targetFind, Entity entity)
    {
        NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.TempJob);
        // if (collisionWorld.OverlapSphere(localTransform.Position, targetRadius.value, ref hits, CollisionFilter.Default))
        // {
        //     // Debug.Log("B");
        //     foreach (var hit in hits)
        //     {
        //         if (!teamLookup.TryGetComponent(hit.Entity, out var unit)) continue;
        //         if (unit.faction == targetFind.value)
        //         {
        //             target.value = hit.Entity;
        //             // Debug.Log(hit.Entity);
        //         }
        //     }
        // }
        // for (int i = 0; i < collisionWorld.NumBodies; i++)
        //     Debug.Log($"Body[{i}]: {collisionWorld.Bodies[i].Entity}");

        if (collisionWorld.OverlapSphere(localTransform.Position, targetRadius.value, ref hits, collisionFilter))
        {
            float closestDistance = float.MaxValue;
            Entity closestEntity = Entity.Null;
            foreach (var hit in hits)
            {
                // target.entityTarget = hit.Entity;
                if (!teamLookup.TryGetComponent(hit.Entity, out var unit)) continue;
                if (unit.faction == teamLookup[entity].faction) continue;
                // Debug.Log(hit.Entity);
                // if (unit.faction == targetFind.value)
                {
                    if (currentHealthLookup.HasComponent(hit.Entity))
                    {
                        var currentHealth = currentHealthLookup[hit.Entity];
                        if (currentHealth.value <= 0)
                        {
                            continue;
                        }
                    }
                    if (hit.Distance < closestDistance)
                    {
                        closestDistance = hit.Distance;
                        closestEntity = hit.Entity;
                    }
                }
            }
            target.value = closestEntity;
            if (currentHealthLookup.HasComponent(entity))
            {
                var currentHealth = currentHealthLookup[entity];
                if (currentHealth.value <= 0)
                {
                    target.value = Entity.Null;
                }
            }
        }
        else
        {
            target.value = Entity.Null;
        }
        hits.Dispose();
    }
}
