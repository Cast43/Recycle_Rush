using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
// [UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct EnemyFollowSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        // state.RequireForUpdate<PlayerInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Certifica-se de que qualquer job pendente foi concluído antes de prosseguir
        // state.Dependency.Complete();

        InputEnemyJob inputEnemyJob = new InputEnemyJob
        {
            transformLookup = SystemAPI.GetComponentLookup<LocalTransform>(true)
        };
        var jobHandler = inputEnemyJob.ScheduleParallel(state.Dependency);
        state.Dependency = jobHandler;
        jobHandler.Complete();

        // Atualiza a dependência do sistema
    }
}

[BurstCompile]
public partial struct InputEnemyJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<LocalTransform> transformLookup;
    [BurstCompile]
    public void Execute(ref Movement movement, ref Direction direction, in TargetEntity target, in LocalTransform localTransform)
    {
        if (target.value == Entity.Null) return;
        float3 inputVector = float3.zero;

        if (target.value != Entity.Null)
        {
            inputVector = transformLookup[target.value].Position - localTransform.Position;
        }

        inputVector.y = 0;
        inputVector = math.normalizesafe(inputVector);
        movement.moveVector = inputVector;
        // Debug.Log(math.lengthsq(inputVector));
        if (math.lengthsq(inputVector) > 0)
        {
            direction.lookDirection = movement.moveVector;
            // Debug.Log("rota");
        }
    }
}
