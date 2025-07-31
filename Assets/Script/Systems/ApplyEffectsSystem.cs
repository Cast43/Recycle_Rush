using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
// [UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ApplyEffectsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
        // state.RequireForUpdate<PoisonEffect>();
        // state.RequireForUpdate<CurseEffect>();
    }

    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        var currentTick = networkTime.ServerTick;

        var toDestroy = new NativeList<Entity>(Allocator.Temp);// lista das entidades ques serão destruidas 

        //Poisoned// 
        foreach (var (poisonProperties, target, entity) in SystemAPI.Query<RefRW<PoisonEffect>, RefRW<EffectTarget>>().WithEntityAccess())
        {
            var poisonDurationBuffLookup = SystemAPI.GetBufferLookup<PoisonDuration>();
            var poisonDpsLookup = SystemAPI.GetBufferLookup<PoisonDps>();
            var poisonDurationBuff = new DynamicBuffer<PoisonDuration>();
            var poisonDpsBuff = new DynamicBuffer<PoisonDps>();

            //verifica se existe poison no alvo
            if (poisonDurationBuffLookup.HasBuffer(target.ValueRO.Value))
            {
                //adiciona os stacks de curse //valta value RW da health
                poisonDurationBuff = SystemAPI.GetBufferLookup<PoisonDuration>()[target.ValueRO.Value];
                poisonDpsBuff = SystemAPI.GetBufferLookup<PoisonDps>()[target.ValueRO.Value];
                poisonDurationBuff.Clear();
            }
            else
            {
                //adiciona os stacks de curse
                poisonDurationBuff = ECB.AddBuffer<PoisonDuration>(target.ValueRO.Value);
                poisonDpsBuff = ECB.AddBuffer<PoisonDps>(target.ValueRO.Value);
            }
            //Adiciona o tempo de duração da poison
            var newPoisonDurationTick = currentTick;
            newPoisonDurationTick.Add(poisonProperties.ValueRO.duration);
            poisonDurationBuff.AddCommandData(new PoisonDuration
            {
                Tick = currentTick,
                value = newPoisonDurationTick
            });
            //Adiciona o tempo do começo do PoisonDps(tick)
            var newPoisonDpsTick = currentTick;
            newPoisonDpsTick.Add(poisonProperties.ValueRO.dmgInterval);
            poisonDpsBuff.AddCommandData(new PoisonDps
            {
                Tick = currentTick,
                value = newPoisonDpsTick,
                damagePerTick = poisonProperties.ValueRO.damagePerTick,
                dmgInterval = poisonProperties.ValueRO.dmgInterval,

            });
            // Debug.Log(entity);
            toDestroy.Add(entity);
            // ECB.AddComponent<DestroyEntityTag>(entity);
            // ECB.DestroyEntity(entity);
        }
        //calcula o tempo da poison e dps duration 
        foreach (var (poisonDurationBuff, poisonDpsBuff, entity) in SystemAPI.Query<DynamicBuffer<PoisonDuration>, DynamicBuffer<PoisonDps>>().WithEntityAccess())
        {
            // Verifica se o poison já expirou
            if (!poisonDurationBuff.GetDataAtTick(currentTick, out var poisonDurationElemnt))
            {
                poisonDurationElemnt.value = NetworkTick.Invalid;
            }
            bool endPoisonDuration = !poisonDurationElemnt.value.IsValid || currentTick.IsNewerThan(poisonDurationElemnt.value);
            // verifica se a duração das curses não acabou
            if (!poisonDpsBuff.GetDataAtTick(currentTick, out var poisonDpsElement))
            {
                poisonDpsElement.value = NetworkTick.Invalid;
            }
            bool dealDamage = !poisonDpsElement.value.IsValid || currentTick.IsNewerThan(poisonDpsElement.value);
            // var ArrowEffectsLookup = SystemAPI.GetComponentLookup<CurseStackEffect>();
            if (dealDamage)
            {
                if (SystemAPI.HasComponent<CurrentHealth>(entity))
                {
                    var health = SystemAPI.GetComponentRW<CurrentHealth>(entity);
                    health.ValueRW.value -= (int)(poisonDpsElement.damagePerTick);
                }
                //Adiciona o tempo do começo do PoisonDps(tick)
                var newPoisonDpsTick = currentTick;
                newPoisonDpsTick.Add(poisonDpsElement.dmgInterval);
                poisonDpsBuff.AddCommandData(new PoisonDps
                {
                    Tick = currentTick,
                    value = newPoisonDpsTick,
                    damagePerTick = poisonDpsElement.damagePerTick,
                    dmgInterval = poisonDpsElement.dmgInterval,

                });
            }
            if (endPoisonDuration)
            {
                ECB.RemoveComponent<PoisonDuration>(entity);
                ECB.RemoveComponent<PoisonDps>(entity);
            }
        }
        //Cursed//
        //adiciona stack de curse//
        foreach (var (curseProperties, target, entity) in SystemAPI.Query<RefRW<CurseEffect>, RefRW<EffectTarget>>().WithEntityAccess().WithNone<DestroyEntityTag>())
        {
            var curseStackEffectLookup = SystemAPI.GetComponentLookup<CurseStackEffect>();
            var curseDurationBuff = new DynamicBuffer<CurseDuration>();

            //verifica se existe curse no alvo
            if (curseStackEffectLookup.HasComponent(target.ValueRO.Value))
            {
                //adiciona os stacks de curse //valta value RW da health
                var curseStack = SystemAPI.GetComponentRW<CurseStackEffect>(target.ValueRO.Value);
                curseStack.ValueRW.value += curseProperties.ValueRO.addAmmount;
                curseDurationBuff = SystemAPI.GetBufferLookup<CurseDuration>()[target.ValueRO.Value];
                curseDurationBuff.Clear();
            }
            else
            {
                //adiciona os stacks de curse
                ECB.AddComponent(target.ValueRO.Value, new CurseStackEffect
                { value = curseProperties.ValueRO.addAmmount, maxStack = curseProperties.ValueRO.maxStacks });
                curseDurationBuff = ECB.AddBuffer<CurseDuration>(target.ValueRO.Value);
            }
            //Adiciona o tempo do começo da curse(tick)
            var newCurseDurationTick = currentTick;
            newCurseDurationTick.Add(curseProperties.ValueRO.duration);
            //Adiciona o tempo de começo do dano da cure
            curseDurationBuff.AddCommandData(new CurseDuration { Tick = currentTick, value = newCurseDurationTick });
            // Debug.Log(entity);
            toDestroy.Add(entity);
            // ECB.AddComponent<DestroyEntityTag>(entity);
            // ECB.DestroyEntity(entity);
        }
        //calcula o tempo da curse duration 
        foreach (var (curseStack, curseDurationBuff, entity) in SystemAPI.Query<RefRW<CurseStackEffect>, DynamicBuffer<CurseDuration>>().WithEntityAccess())
        {
            // verifica se a duração das curses não acabou
            if (!curseDurationBuff.GetDataAtTick(currentTick, out var curseDuration))
            {
                curseDuration.value = NetworkTick.Invalid;
            }
            bool endCurseDuration = !curseDuration.value.IsValid || currentTick.IsNewerThan(curseDuration.value);
            //se o stack de curse chegou no maximo ele toma o dano dos stacks
            //se acabou o tempo de cursed
            // Debug.Log("duration " + endCurseDuration);
            // Debug.Log("maxStacks " + (bool)(curseStack.ValueRO.value >= curseStack.ValueRO.maxStack));
            if (endCurseDuration || curseStack.ValueRO.value >= curseStack.ValueRO.maxStack)
            {
                if (SystemAPI.HasComponent<CurrentHealth>(entity))
                {
                    var health = SystemAPI.GetComponentRW<CurrentHealth>(entity);
                    health.ValueRW.value -= (int)(curseStack.ValueRO.value);
                    curseStack.ValueRW.value = 0;
                    ECB.RemoveComponent<CurseStackEffect>(entity);
                    ECB.RemoveComponent<CurseDuration>(entity);
                }
            }
        }
        //lightning
        foreach (var (lightningProperties, target, entity) in SystemAPI.Query<RefRW<LightningEffect>, RefRW<EffectTarget>>().WithEntityAccess())
        {
            var lightningDurationLookup = SystemAPI.GetBufferLookup<LightningDuration>();
            var lightningDpsLookup = SystemAPI.GetBufferLookup<LightningDps>();
            var lightningDurationBuff = new DynamicBuffer<LightningDuration>();
            var lightningDpsBuff = new DynamicBuffer<LightningDps>();

            //verifica se existe lightning no alvo
            if (lightningDurationLookup.HasBuffer(target.ValueRO.Value))
            {
                lightningDurationBuff = lightningDurationLookup[target.ValueRO.Value];
                lightningDpsBuff = lightningDpsLookup[target.ValueRO.Value];
                //limpa o tempo atual de duração e cria um novo tempo para o buffer(reseta a duração)
                lightningDurationBuff.Clear();
            }
            else
            {
                //adiciona os stacks de curse
                lightningDurationBuff = ECB.AddBuffer<LightningDuration>(target.ValueRO.Value);
                lightningDpsBuff = ECB.AddBuffer<LightningDps>(target.ValueRO.Value);
            }
            //Adiciona o tempo do começo do lightning(tick)
            var newLightningDurationTick = currentTick;
            newLightningDurationTick.Add(lightningProperties.ValueRO.duration);
            //Adiciona o tempo de começo do dano do lightning
            lightningDurationBuff.AddCommandData(new LightningDuration
            {
                Tick = currentTick,
                value = newLightningDurationTick,
                radius = lightningProperties.ValueRO.radius,
                target = lightningProperties.ValueRO.target,
                effectGiver = target.ValueRO.effectGiver
            });

            //Adiciona o tempo do começo do lightninDps(tick)
            var newLightningDpsTick = currentTick;
            newLightningDpsTick.Add(lightningProperties.ValueRO.dmgInterval);
            //Adiciona o tempo de começo do dano do lightning

            lightningDpsBuff.AddCommandData(new LightningDps
            {
                Tick = currentTick,
                value = newLightningDpsTick,
                damagePerTick = lightningProperties.ValueRO.damagePerTick,
                dmgInterval = lightningProperties.ValueRO.dmgInterval,

            });

            toDestroy.Add(entity);
            // ECB.DestroyEntity(entity);
        }
        //calcula o tempo da lightning duration 
        foreach (var (LightningDurationBuff, LightningDpsBuff, hitPos, entity) in SystemAPI.Query<DynamicBuffer<LightningDuration>, DynamicBuffer<LightningDps>, RefRO<LocalTransform>>().WithEntityAccess())
        {
            // Verifica se o lightningDuration já expirou
            if (!LightningDurationBuff.GetDataAtTick(currentTick, out var lightningDurationElemnt))
            {
                lightningDurationElemnt.value = NetworkTick.Invalid;
            }
            bool endLightningDuration = !lightningDurationElemnt.value.IsValid || currentTick.IsNewerThan(lightningDurationElemnt.value);
            // verifica se a duração da lightningDps não acabou
            if (!LightningDpsBuff.GetDataAtTick(currentTick, out var lightningDpsElement))
            {
                lightningDpsElement.value = NetworkTick.Invalid;
            }
            bool dealDamage = !lightningDpsElement.value.IsValid || currentTick.IsNewerThan(lightningDpsElement.value);
            // var ArrowEffectsLookup = SystemAPI.GetComponentLookup<CurseStackEffect>();
            if (dealDamage)
            {
                ///sistema para acerto de inimigos dentro do raio
                // ponto central e raio do overlap
                // resultados encontrados
                NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
                // var hits = new NativeList<ColliderHit>(Allocator.Temp);
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                //cria uma esfera para achar entidades no entorno
                CollisionFilter collisionFilter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.UNIT_LAYER,
                    GroupIndex = 0
                    // CollidesWith = (1u << GameAssets.PLAYER_LAYER) | (1u << GameAssets.ENEMY_LAYER),
                };
                // var hitsList = new NativeList<DistanceHit>(Allocator.Temp);

                if (collisionWorld.OverlapSphere(hitPos.ValueRO.Position, lightningDurationElemnt.radius, ref hits, collisionFilter))
                {
                    foreach (var hit in hits)
                    {
                        Entity hitEntity = hit.Entity;
                        if (SystemAPI.HasComponent<CurrentHealth>(hitEntity))
                        {
                            if (SystemAPI.HasComponent<Team>(hitEntity))
                            {
                                var team = SystemAPI.GetComponentRO<Team>(hitEntity);
                                if (team.ValueRO.faction == lightningDurationElemnt.target)
                                {
                                    var health = SystemAPI.GetComponentRW<CurrentHealth>(hitEntity);
                                    health.ValueRW.value -= (int)(lightningDpsElement.damagePerTick);

                                    if (hitEntity != entity)
                                    {
                                        var giverEffectsBuff = SystemAPI.GetBufferLookup<EffectPrefab>();
                                        if (giverEffectsBuff.HasBuffer(lightningDurationElemnt.effectGiver))
                                        {
                                            var effects = giverEffectsBuff[lightningDurationElemnt.effectGiver];

                                            // Para cada prefab de efeito configurado, instancia uma cópia para o alvo
                                            foreach (var prefabElem in effects)
                                            {
                                                Debug.Log(hitEntity);
                                                var effectInstance = ECB.Instantiate(prefabElem.Prefab);
                                                // Associa o efeito ao alvo, via um componente “Target” ou via Parent
                                                ECB.AddComponent(effectInstance, new EffectTarget { Value = hitEntity, effectGiver = lightningDurationElemnt.effectGiver });
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                hits.Dispose();

                //Adiciona o tempo do começo do lightninDps(tick)
                var lightningDpsTick = currentTick;
                lightningDpsTick.Add(lightningDpsElement.dmgInterval);
                //adiciona o tempo do próximo dps para lightning
                LightningDpsBuff.AddCommandData(new LightningDps
                {
                    Tick = currentTick,
                    value = lightningDpsTick,
                    damagePerTick = lightningDpsElement.damagePerTick,
                    dmgInterval = lightningDpsElement.dmgInterval
                });
                //adicionar o tempo do próximo tick
            }
            if (endLightningDuration)
            {
                ECB.RemoveComponent<LightningDps>(entity);
                ECB.RemoveComponent<LightningDuration>(entity);
            }

        }
        //ponto que vai destruir os efeitos
        for (int i = 0; i < toDestroy.Length; i++)
            ECB.DestroyEntity(toDestroy[i]);

        toDestroy.Dispose();
    }
}
