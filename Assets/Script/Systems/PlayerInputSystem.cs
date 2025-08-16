using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(GhostInputSystemGroup))]
partial struct PlayerInputSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<PlayerInput>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {


        float3 inputVector = new float3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        // Debug.Log($"InputVector: {inputVector}  Magnitude: {math.length(inputVector)}");
        inputVector = math.normalizesafe(inputVector);


        bool shoot = Input.GetButton("Fire1");
        bool dash = Input.GetButton("Fire2");

        var inputJob = new PlayerInputJob
        {
            inputVector = inputVector,
            shoot = shoot,
            dash = dash,
        };

        state.Dependency = inputJob.ScheduleParallel(state.Dependency);

        // foreach (
        //     (RefRW<PlayerInput> playerInput,
        //     RefRW<Movement> movement,
        //     RefRW<Direction> direction)
        //     in SystemAPI.Query<
        //         RefRW<PlayerInput>,
        //         RefRW<Movement>,
        //         RefRW<Direction>>().WithAll<GhostOwnerIsLocal>())
        // {//foreach
        //     float3 inputVector = new float3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        //     inputVector = math.normalizesafe(inputVector);
        //     movement.ValueRW.moveVector = inputVector;
        //     if (math.lengthsq(inputVector) > 0)
        //     {
        //         direction.ValueRW.lookDirection = movement.ValueRO.moveVector;
        //     }
        //     if (Input.GetMouseButtonDown(0))
        //     {
        //         playerInput.ValueRW.shoot.Set();
        //     }
        //     else
        //     {
        //         playerInput.ValueRW.shoot = default;
        //     }
        // }
    }
}

// [WithNone(typeof(NeedRessurection))]
public partial struct PlayerInputJob : IJobEntity
{
    public float3 inputVector;
    public bool shoot;
    public bool dash;

    public void Execute(ref PlayerInput playerInput, ref MovementPlayer movement, ref Direction direction, CurrentHealth currentHealth, in GhostOwnerIsLocal owner)
    {
        if (currentHealth.value <= 0)
        {
            inputVector = float3.zero;
            shoot = false;
            dash = false;
        }
        movement.moveVector = inputVector;
        if (math.lengthsq(inputVector) > 0)
        {
            direction.lookDirection = movement.moveVector;
        }
        if (shoot)
            playerInput.shoot.Set();
        else
            playerInput.shoot = default;

        if (dash)
        {
            playerInput.dash.Set();
        }
        else
            playerInput.dash = default;


    }
}

