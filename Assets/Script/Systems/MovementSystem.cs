using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct MovementSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        PlayerMoveJob playerMoveJob = new PlayerMoveJob { currentTick = networkTime.ServerTick };
        MoveJob MoveJob = new MoveJob
        {
            currentTick = networkTime.ServerTick,
            dontMoveOnTimer = SystemAPI.GetBufferLookup<DontMoveOnTimer>(true),
        };
        RotationJob RotationJob = new RotationJob
        {
            directionLookup = SystemAPI.GetComponentLookup<Direction>(true),
            // teamLookup = SystemAPI.GetComponentLookup<Team>(true),
            deltaTime = SystemAPI.Time.DeltaTime,
        };

        var h1 = playerMoveJob.ScheduleParallel(state.Dependency);
        var h2 = MoveJob.ScheduleParallel(h1);
        var h3 = RotationJob.ScheduleParallel(h2);
        state.Dependency = h3;
    }
}

// [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct MoveJob : IJobEntity
{
    [ReadOnly] public NetworkTick currentTick;
    [ReadOnly] public BufferLookup<DontMoveOnTimer> dontMoveOnTimer;
    public void Execute(in Movement movement, ref PhysicsVelocity physicsVelocity, ref LocalTransform localTransform, in MoveSpeed speed, in Entity entity)
    {
        if (dontMoveOnTimer.TryGetBuffer(entity, out var dontMoveOnTimers))
        {
            if (!dontMoveOnTimers.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }

            bool canMoveNotAttacking = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);
            //DONT MOVE ATTACKING

            if (!canMoveNotAttacking)
            {
                physicsVelocity.Angular = float3.zero;
                physicsVelocity.Linear = float3.zero;
                return;//está no intervalo de preparação ataque e deve esperar
            }
        }

        physicsVelocity.Angular = float3.zero;
        physicsVelocity.Linear = movement.moveVector * speed.value;
        localTransform.Rotation = quaternion.identity;
    }
}
public partial struct PlayerMoveJob : IJobEntity
//como o player usa IInputComponentData deve existir um job de moviemnto apenas para ele
{
    [ReadOnly] public NetworkTick currentTick;

    public void Execute(in MovementPlayer movement, ref PhysicsVelocity physicsVelocity, ref LocalTransform localTransform, in MoveSpeed speed, DynamicBuffer<DashDuration> dashDuration)
    {
        //enquanto estiver no dash não pode andar
        if (!dashDuration.IsEmpty)
        {
            if (!dashDuration.GetDataAtTick(currentTick, out var dashDurationTick))
            {
                dashDurationTick.value = NetworkTick.Invalid;
            }
            bool inDash = !dashDurationTick.value.IsValid || !currentTick.IsNewerThan(dashDurationTick.value);
            if (inDash) return;
        }

        physicsVelocity.Angular = float3.zero;
        physicsVelocity.Linear = movement.moveVector * speed.value;
        localTransform.Rotation = quaternion.identity;
    }
}

public partial struct RotationJob : IJobEntity
{
    [ReadOnly] public ComponentLookup<Direction> directionLookup;
    // [ReadOnly] public ComponentLookup<Team> teamLookup;
    public float deltaTime;

    public void Execute(in Rotation rotation, ref LocalTransform localTransform, in Parent parent)
    {
        // return;
        Direction direction = directionLookup[parent.Value];

        float3 lookDirection = direction.lookDirection;
        if (math.lengthsq(lookDirection) <= 0)
            return;

        lookDirection.y = 0;

        // Gira 90 graus em torno de Y
        quaternion offset = quaternion.RotateY(math.radians(rotation.Yoffset));

        // Rotação alvo com offset aplicado
        quaternion targetRotation = math.mul(quaternion.LookRotationSafe(lookDirection, math.up()), offset);
        // Debug.Log(targetRotation);

        // Interpolação esférica (suavização)
        localTransform.Rotation = math.slerp(localTransform.Rotation, targetRotation, deltaTime * (rotation.rotationSpeed));
    }
}