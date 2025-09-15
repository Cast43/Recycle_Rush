using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

// [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameDamageSystem))]
[WithNone(typeof(DestroyEntityTag))]
partial struct ApplyDamageSystem : ISystem
{
    // [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();

    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // bool isServer = state.WorldUnmanaged.IsServer();
        // bool isClient = state.WorldUnmanaged.IsClient();
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        // BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        // EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);       
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);
        ComponentLookup<DropExperienceEntity> dropExperienceComponentLookup = SystemAPI.GetComponentLookup<DropExperienceEntity>();
        ComponentLookup<RessurectArea> ressurectAreaLookup = SystemAPI.GetComponentLookup<RessurectArea>();
        ComponentLookup<RessurectProperties> ressurectPropertiesLookup = SystemAPI.GetComponentLookup<RessurectProperties>();

        foreach (var (currentHealth, damageThisTickBuffer, localTransform, entity) in
            SystemAPI.Query<RefRW<CurrentHealth>, DynamicBuffer<DamageThisTick>, RefRO<LocalTransform>>().WithNone<NeedRessurection>().WithAll<Simulate>().WithEntityAccess())
        {
            if (!damageThisTickBuffer.GetDataAtTick(currentTick, out var damageThisTick)) continue;
            if (damageThisTick.Tick != currentTick) continue;
            //atualiza a vida atual
            if (currentHealth.ValueRO.value > 0)
            {
                currentHealth.ValueRW.value -= damageThisTick.value;

            }

            if (damageThisTick.value > 0)
            {
                // Debug.Log("teste apply");
                currentHealth.ValueRW.onHealthChanged = true;
            }

            if (currentHealth.ValueRO.value <= 0)
            {
                // Debug.Log(entity);
                if (dropExperienceComponentLookup.HasComponent(entity))
                {
                    // if (isServer)
                    {
                        var dropExperience = dropExperienceComponentLookup[entity];

                        var dropEntity = ECB.Instantiate(dropExperience.value);
                        ECB.SetComponent(dropEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position + new float3(0, 0.5f, 0)));
                        // ECB.AppendToBuffer(damageThisTick.owner, new ExperienceBufferElement { value = giverExperience.value });
                        // ECB.SetComponent(damageThisTick.owner, levelOwner);
                    }
                }
                if (ressurectAreaLookup.HasComponent(entity))
                {
                    // if (isServer)
                    {
                        var ressurectArea = ressurectAreaLookup[entity];
                        Entity ressurectionArea = ECB.Instantiate(ressurectArea.ressurectionArea);
                        var ressurectProperties = ressurectPropertiesLookup[ressurectArea.ressurectionArea];
                        // ECB.AddComponent(ressurectionArea, new DestroyOnTimer { value = ressurectArea.maxRessurectionDuration });
                        // var timeInAreaBuff = ECB.AddBuffer<TimeInRessurectionArea>(ressurectionArea);
                        // cria ressurectTimeInArea 
                        //cria ressurect duration
                        var ressurectDurationBuff = ECB.AddBuffer<RessurectionDuration>(ressurectionArea);
                        var newRessurectDurationTick = currentTick;
                        newRessurectDurationTick.Add(ressurectProperties.maxRessurectionDuration);
                        ressurectDurationBuff.AddCommandData(new RessurectionDuration
                        {
                            Tick = currentTick,
                            value = newRessurectDurationTick,
                            ressurectedEntity = entity,
                        });

                        // ECB.AddComponent(ressurectionArea, new Team { faction = ressurectArea.team });
                        ECB.SetComponent(ressurectionArea, LocalTransform.FromPosition(localTransform.ValueRO.Position - new float3(0, localTransform.ValueRO.Position.y, 0)));
                    }
                    ECB.AddComponent<NeedRessurection>(entity);

                }
                else
                {
                    // if (isServer)
                    {
                        ECB.AddComponent<DestroyEntityTag>(entity);
                    }
                }
            }
        }
        ECB.Playback(state.EntityManager);
    }
}
