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
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;
        // BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        // EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);       
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);
        BufferLookup<DropExperienceEntity> dropExperienceBufferLookup = SystemAPI.GetBufferLookup<DropExperienceEntity>();
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
                // Debug.Log(damageThisTick.value);
                currentHealth.ValueRW.onHealthChanged = true;

                var damageFlashLookup = SystemAPI.GetComponentLookup<DamageFlashTimer>();
                // Define que o personagem ficará vermelho por 0.2 segundos
                if (damageFlashLookup.HasComponent(entity))
                {
                    var flashDuration = SystemAPI.GetComponentLookup<DamageFlashTimer>()[entity].maxDuration;

                    damageFlashLookup.GetRefRW(entity).ValueRW.Value = flashDuration;
                }

            }

            if (currentHealth.ValueRO.value <= 0)
            {
                // Debug.Log(entity);
                if (dropExperienceBufferLookup.HasBuffer(entity))
                {
                    if (!SystemAPI.HasComponent<AlreadySpawnedXPTag>(entity))
                    {
                        var dropExperience = dropExperienceBufferLookup[entity];

                        uint seed = (uint)System.Diagnostics.Stopwatch.GetTimestamp();

                        var randomGenerator = new Unity.Mathematics.Random(math.max(1, seed));

                        int randNumber = randomGenerator.NextInt(0, dropExperience.Length);

                        var dropEntity = ECB.Instantiate(dropExperience[randNumber].value);
                        ECB.SetComponent(dropEntity, LocalTransform.FromPosition(localTransform.ValueRO.Position + new float3(0, 0.5f, 0)));
                        // Debug.Log("SpawnXP");
                        // ECB.AppendToBuffer(damageThisTick.owner, new ExperienceBufferElement { value = giverExperience.value });
                        // ECB.SetComponent(damageThisTick.owner, levelOwner);
                        ECB.AddComponent<AlreadySpawnedXPTag>(entity);
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
                    ECB.AddComponent<DestroyEntityTag>(entity);
                }
            }
        }
        ECB.Playback(state.EntityManager);
    }
}
