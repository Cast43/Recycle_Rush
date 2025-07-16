using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct ArrowSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Allocator.Temp);

        // Obtenha o PhysicsWorldSingleton para acessar o CollisionWorld.
        PhysicsWorldSingleton physicsWorldSingleton = SystemAPI.GetSingleton<PhysicsWorldSingleton>();
        CollisionWorld collisionWorld = physicsWorldSingleton.CollisionWorld;

        state.Dependency = new ArrowJob
        {
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true),
        }.ScheduleParallel(state.Dependency);
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
