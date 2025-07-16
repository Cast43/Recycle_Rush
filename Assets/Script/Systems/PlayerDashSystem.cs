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
        PlayerDashJob playerDashJob = new PlayerDashJob { currentTick = networkTime.ServerTick };

        var h1 = setDashVector.ScheduleParallel(state.Dependency);
        var h2 = playerDashJob.ScheduleParallel(h1);
        // state.Dependency = setDashVector.ScheduleParallel(state.Dependency);
        // state.Dependency = playerDashJob.ScheduleParallel(state.Dependency);
        state.Dependency = h2;
    }

    public partial struct SetDashVector : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;

        public void Execute(ref DashVector dash, ref DynamicBuffer<DashCooldown> dashCooldown, in DashProperties dashProperties, in LocalTransform localTransform, in MovementPlayer movementPlayer, in PlayerInput playerInput, DynamicBuffer<DashDuration> dashDuration)
        {
            if (!playerInput.dash.IsSet) return; //verifica se foi apertado o botao de dash
            dash.dashVector = movementPlayer.moveVector;

            //verifica se ja não está no dash
            if (!dashDuration.IsEmpty)
            {
                if (!dashDuration.GetDataAtTick(currentTick, out var dashDurationTick))
                {
                    dashDurationTick.value = NetworkTick.Invalid;
                }
                bool inDash = !dashDurationTick.value.IsValid || !currentTick.IsNewerThan(dashDurationTick.value);
                // Debug.Log("dashDuration is newer than: " + !currentTick.IsNewerThan(cooldownExpirationTickD.value));
                if (inDash) return;
            }

            //verifica se não está no cooldown
            if (!dashCooldown.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }
            bool canDash = !cooldownExpirationTick.value.IsValid || currentTick.IsNewerThan(cooldownExpirationTick.value);

            if (!canDash) return;
            Debug.Log("candash " + canDash);
            //////
            /// 

            //essa parte do código da problema junto com a parte de cima
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
    public partial struct PlayerDashJob : IJobEntity
    {
        [ReadOnly] public NetworkTick currentTick;

        public void Execute(ref PhysicsVelocity physicsVelocity, ref LocalTransform localTransform, DynamicBuffer<DashDuration> dashDuration, in PlayerInput playerInput, in DashVector dashVector, in DashProperties dashProperties)
        {
            if (dashDuration.IsEmpty) return;

            if (!dashDuration.GetDataAtTick(currentTick, out var cooldownExpirationTick))
            {
                cooldownExpirationTick.value = NetworkTick.Invalid;
            }
            bool canDash = !cooldownExpirationTick.value.IsValid || !currentTick.IsNewerThan(cooldownExpirationTick.value);

            if (!canDash) return;

            physicsVelocity.Angular = float3.zero;
            physicsVelocity.Linear = dashVector.dashVector * dashProperties.speed;
            localTransform.Rotation = quaternion.identity;
        }
    }
}