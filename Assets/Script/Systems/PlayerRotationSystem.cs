using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

// [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct PlayerRotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<PlayerInput>();
    }
    public void OnUpdate(ref SystemState state)
    {
        foreach (var (input, movement, direction) in
         SystemAPI.Query<RefRO<PlayerInput>, RefRW<MovementPlayer>, RefRW<Direction>>())
        {
            // movement.ValueRW.moveVector = input.ValueRO.movement;

            if (math.lengthsq(movement.ValueRO.moveVector) > 0)
            {
                direction.ValueRW.lookDirection = movement.ValueRO.moveVector;
            }
        }
    }
}
