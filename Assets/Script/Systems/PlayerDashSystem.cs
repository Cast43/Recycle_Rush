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
        PlayerDashJob playerDashJob = new PlayerDashJob { };

        var h1 = setDashVector.ScheduleParallel(state.Dependency);
        var h2 = verifyCanDash.ScheduleParallel(h1);
        var h3 = playerDashJob.ScheduleParallel(h2);
        // state.Dependency = setDashVector.ScheduleParallel(state.Dependency);
        // state.Dependency = playerDashJob.ScheduleParallel(state.Dependency);
        state.Dependency = h3;
    }

    public partial struct VerifyCanDash : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;
        public void Execute(ref DashVector dash, ref DynamicBuffer<DashCooldown> dashCooldown, ref DashProperties dashProperties, in LocalTransform localTransform, in MovementPlayer movementPlayer, in PlayerInput playerInput, DynamicBuffer<DashDuration> dashDuration)
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
    [UpdateAfter(typeof(VerifyCanDash))]
    public partial struct SetDashVector : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;

        public void Execute(ref DashVector dashVector, ref DynamicBuffer<DashCooldown> dashCooldown, in DashProperties dashProperties, in LocalTransform localTransform, in MovementPlayer movementPlayer, in PlayerInput playerInput, ref DynamicBuffer<DashDuration> dashDuration)
        {
            if (!playerInput.dash.IsSet) return; //verifica se foi apertado o botao de dash
            if (dashProperties.isDashing) return;
            if (!dashProperties.canDash) return;
            if (math.lengthsq(movementPlayer.moveVector) == 0) return;

            dashVector.dashVector = movementPlayer.moveVector;

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

        public void Execute(ref PhysicsVelocity physicsVelocity, ref LocalTransform localTransform, DynamicBuffer<DashDuration> dashDuration, in PlayerInput playerInput, in DashVector dashVector, in DashProperties dashProperties)
        {
            if (!dashProperties.isDashing) return;

            physicsVelocity.Angular = float3.zero;
            physicsVelocity.Linear = dashVector.dashVector * dashProperties.speed;
            localTransform.Rotation = quaternion.identity;
        }
    }
}