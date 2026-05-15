using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;
using Unity.Collections;

// Movido para PredictedSimulationSystemGroup para rodar durante o pause e poder zerar a velocidade
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
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

        state.Dependency = new ArrowMoveJob
        {
            isPaused = isPaused
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(Simulate))]
public partial struct ArrowMoveJob : IJobEntity
{
    [ReadOnly] public bool isPaused;

    public void Execute(ref PhysicsVelocity physicsVelocity, in Arrow arrow, in Direction direction)
    {
        if (isPaused)
        {
            physicsVelocity.Linear = float3.zero;
            physicsVelocity.Angular = float3.zero;
            return;
        }

        physicsVelocity.Linear = direction.lookDirection * arrow.moveSpeed;
        physicsVelocity.Angular = float3.zero;
    }
}
