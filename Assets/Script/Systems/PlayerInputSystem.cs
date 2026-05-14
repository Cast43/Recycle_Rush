using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using UnityEngine;

// Movido para PredictedSimulationSystemGroup para que possa rodar durante o pause e zerar o input.
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct PlayerInputSystem : ISystem
{
    [BurstCompile] // O OnCreate pode continuar com Burst
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<PlayerInput>();
    }

    // Remova o BurstCompile daqui! Classes da Unity (UnityEngine.Input) quebram o compilador
    public void OnUpdate(ref SystemState state)
    {
        bool isPaused = false;
        // Verifica se o jogo está pausado. Isso funciona no cliente e no servidor.
        if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState))
        {
            isPaused = matchState.IsPaused;
        }

        // Se estiver pausado, o vetor de input é forçado a zero.
        // Caso contrário, lê o input do jogador normalmente.
        float3 inputVector = isPaused ? float3.zero : new float3(Input.GetAxisRaw("Horizontal"), 0, Input.GetAxisRaw("Vertical"));
        // Debug.Log($"InputVector: {inputVector}  Magnitude: {math.length(inputVector)}");
        inputVector = math.normalizesafe(inputVector);


        bool shoot = isPaused ? false : Input.GetButton("Fire1");
        bool dash = isPaused ? false : Input.GetButton("Fire2");

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
        // if (math.lengthsq(inputVector) > 0)
        // {
        //     direction.lookDirection = inputVector;
        // }
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
