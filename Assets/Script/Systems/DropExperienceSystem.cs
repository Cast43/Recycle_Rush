using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct DropExperienceSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();

        // foreach (var (dropExperience, currentHealth, localTransform, entity) in SystemAPI.Query<RefRO<DropExperienceEntity>, RefRO<CurrentHealth>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            // if (currentHealth.ValueRO.value <= 0)
            {
                // var dropEntity = ECB.Instantiate(dropExperience.ValueRO.value);
                // ECB.SetComponent(dropEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position + new float3(0, 0.5f, 0)));
                // ECB.AddComponent<DestroyEntityTag>(entity);
            }
        }
        // state.Dependency = new DropExperienceJob
        // {
        //     ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
        // }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct DropExperienceJob : IJobEntity
{
    public EntityCommandBuffer.ParallelWriter ECB;

    [BurstCompile]
    public void Execute(in DropExperienceEntity dropExperience, in CurrentHealth currentHealth, LocalTransform localTransform, Entity entity, [ChunkIndexInQuery] int sortKey)
    {
        if (currentHealth.value <= 0)
        {
            Debug.Log(entity);
            var dropEntity = ECB.Instantiate(sortKey, dropExperience.value);
            ECB.SetComponent(sortKey, dropEntity, LocalTransform.FromPosition(localTransform.Position + new float3(0, 0.5f, 0)));
        }
    }
}
