using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Physics;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
partial struct PlayerDashSystem : ISystem
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
        SetDashVector setDashVector = new SetDashVector { currentTick = networkTime.ServerTick };
        VerifyCanDash verifyCanDash = new VerifyCanDash { currentTick = networkTime.ServerTick };
        PlayerDashJob playerDashJob = new PlayerDashJob { currentTick = networkTime.ServerTick };

        var h1 = setDashVector.ScheduleParallel(state.Dependency);
        var h2 = verifyCanDash.ScheduleParallel(h1);
        var h3 = playerDashJob.ScheduleParallel(h2);

        state.Dependency = h3;
    }
    public partial struct VerifyCanDash : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;
        public void Execute(ref DashProperties dashProperties, in LocalTransform localTransform, in PlayerInput playerInput,
                            DynamicBuffer<DashDuration> dashDuration, DynamicBuffer<DashCooldown> dashCooldown)
        {
            //verifica se ja não está no dash
            if (!dashDuration.IsEmpty)
            {
                if (!dashDuration.GetDataAtTick(currentTick, out var dashDurationTick))
                {
                    dashDurationTick.value = NetworkTick.Invalid;
                }
                bool isDashing = !dashDurationTick.value.IsValid || !currentTick.IsNewerThan(dashDurationTick.value);
                // Debug.Log("dashDuration is newer than: " + !currentTick.IsNewerThan(cooldownExpirationTickD.value));
                dashProperties.isDashing = isDashing;
            }

            //verifica se não está no cooldown
            if (!dashCooldown.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }
            bool canDash = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);
            dashProperties.canDash = canDash;

        }
    }
    public partial struct SetDashVector : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;

        public void Execute(in DashProperties dashProperties, in LocalTransform localTransform, in PlayerInput playerInput, in MovementPlayer movementPlayer,
                           DynamicBuffer<DashCommand> dashCommandBuffer, DynamicBuffer<DashDuration> dashDuration, DynamicBuffer<DashCooldown> dashCooldown)
        {
            if (!playerInput.dash.IsSet) return; //verifica se foi apertado o botao de dash
            if (dashProperties.isDashing) return;
            if (!dashProperties.canDash) return;
            if (math.lengthsq(movementPlayer.moveVector) == 0) return;

            var newDashCommand = new DashCommand
            {
                Tick = currentTick,
                DashDirection = movementPlayer.moveVector
            };
            dashCommandBuffer.AddCommandData(newDashCommand);

            //adiciona um tempo até poder usar o dash novamente
            var newCooldownTickDash = currentTick;
            newCooldownTickDash.Add(dashProperties.cooldown);
            dashCooldown.AddCommandData(new DashCooldown { Tick = currentTick, value = newCooldownTickDash });

            //Adiciona o tempo de duração do dash tipo. exp 1 sec
            var newDashDurationTick = currentTick;
            newDashDurationTick.Add(dashProperties.duration);
            dashDuration.AddCommandData(new DashDuration { Tick = currentTick, value = newDashDurationTick });

        }
    }
    [UpdateAfter(typeof(SetDashVector))]
    public partial struct PlayerDashJob : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;

        public void Execute(ref PhysicsVelocity physicsVelocity, ref LocalTransform localTransform, in PlayerInput playerInput, in DashProperties dashProperties
                            , DynamicBuffer<DashCommand> dashCommandBuffer)
        {
            if (!dashProperties.isDashing) return;
            if (!dashCommandBuffer.GetDataAtTick(currentTick, out var dashCommand)) return;

            physicsVelocity.Angular = float3.zero;
            physicsVelocity.Linear = dashCommand.DashDirection * dashProperties.speed;
            localTransform.Rotation = quaternion.identity;
        }
    }
}