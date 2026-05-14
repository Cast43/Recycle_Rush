using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using Unity.Collections;

[UpdateInGroup(typeof(PausablePredictedGroup))]
partial struct ArrowSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        bool isPaused = false;
        if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState))
        {
            isPaused = matchState.IsPaused;
        }

        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        // Obtenha o PhysicsWorldSingleton para acessar o CollisionWorld.
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        foreach (var (physicsVelocity, arrow, direction, localTransform, arrowEntity) in
                SystemAPI.Query<RefRW<PhysicsVelocity>, RefRO<Arrow>, RefRO<Direction>, RefRO<LocalTransform>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (isPaused)
            {
                physicsVelocity.ValueRW.Linear = float3.zero;
                physicsVelocity.ValueRW.Angular = float3.zero;
            }
            else
            {
                physicsVelocity.ValueRW.Linear = direction.ValueRO.lookDirection * arrow.ValueRO.moveSpeed;
                physicsVelocity.ValueRW.Angular = float3.zero;
            }
        }

        // state.Dependency = new ArrowJob
        // {
        //     transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
        // }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct ArrowJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;

    [BurstCompile]
    public void Execute(ref PhysicsVelocity physicsVelocity, ref Arrow arrow, ref Direction direction, Entity arrowEntity, [ChunkIndexInQuery] int sortKey)
    {
        if (!transformLookup.HasComponent(arrowEntity)) return;

        physicsVelocity.Linear = direction.lookDirection * arrow.moveSpeed;
        physicsVelocity.Angular = float3.zero;
    }
}
