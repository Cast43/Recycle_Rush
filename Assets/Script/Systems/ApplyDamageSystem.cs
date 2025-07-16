using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameDamageSystem))]
partial struct ApplyDamageSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();

    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);
        ComponentLookup<GiveExperience> giveExperienceLookup = SystemAPI.GetComponentLookup<GiveExperience>();
        BufferLookup<ExperienceBufferElement> experienceBufferLookup = SystemAPI.GetBufferLookup<ExperienceBufferElement>();

        foreach (var (currentHealth, damageThisTickBuffer, entity) in
            SystemAPI.Query<RefRW<CurrentHealth>, DynamicBuffer<DamageThisTick>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (!damageThisTickBuffer.GetDataAtTick(currentTick, out var damageThisTick)) continue;
            if (damageThisTick.Tick != currentTick) continue;
            //atualiza a vida atual
            currentHealth.ValueRW.value -= damageThisTick.value;

            if (damageThisTick.value > 0)
            {
                // Debug.Log("teste apply");
                currentHealth.ValueRW.onHealthChanged = true;
            }

            if (currentHealth.ValueRW.value <= 0)
            {
                if (experienceBufferLookup.HasBuffer(damageThisTick.owner) &&
                    giveExperienceLookup.HasComponent(entity))
                {
                    // var RecieverExperienceBuffer = experienceBufferLookup[damageThisTick.owner];
                    var giverExperience = giveExperienceLookup[entity];

                    ECB.AppendToBuffer(damageThisTick.owner, new ExperienceBufferElement { value = giverExperience.value });
                    // ECB.SetComponent(damageThisTick.owner, levelOwner);
                }
                ECB.AddComponent<DestroyEntityTag>(entity);
            }
        }
        ECB.Playback(state.EntityManager);
    }
}
