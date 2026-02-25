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
// [UpdateBefore(typeof(CalculateFrameDamageSystem))]
[UpdateInGroup(typeof(SimulationSystemGroup))]
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
            // var poisonUnitVisual = new EffectUnitVisual();

            //verifica se existe poison no alvo
            // if (poisonDurationBuffLookup.HasBuffer(target.ValueRO.Value))
            // {
            //     //adiciona os stacks de poison //valta value RW da health
            // poisonDurationBuff = SystemAPI.GetBufferLookup<PoisonDuration>()[target.ValueRO.Value];
            // poisonDpsBuff = SystemAPI.GetBufferLookup<PoisonDps>()[target.ValueRO.Value];
            //     poisonDurationBuff.Clear();
            // }
            // else
            // {
            //instancia o efeito de poison em uma posição
            var poisonEffectArea = ECB.Instantiate(poisonProperties.ValueRO.poisonEffectArea);

            poisonDurationBuff = ECB.AddBuffer<PoisonDuration>(poisonEffectArea);
            poisonDpsBuff = ECB.AddBuffer<PoisonDps>(poisonEffectArea);
            // //lightning visual effect
            var poisonEffectPosition = SystemAPI.GetComponentLookup<LocalTransform>()[target.ValueRO.Value];
            ECB.SetComponent(poisonEffectArea, new PoisonVisualTag { position = poisonEffectPosition.Position });

            ECB.SetComponent(poisonEffectArea, LocalTransform.FromPositionRotationScale(
                 poisonEffectPosition.Position,
                quaternion.identity,
                poisonProperties.ValueRO.areaRadius
            ));
            // }
            // //Adiciona o tempo de duração da poison
            var newPoisonDurationTick = currentTick;
            newPoisonDurationTick.Add(poisonProperties.ValueRO.duration);
            poisonDurationBuff.AddCommandData(new PoisonDuration
            {
                Tick = currentTick,
                value = newPoisonDurationTick,
                duration = poisonProperties.ValueRO.duration,
                radius = poisonProperties.ValueRO.areaRadius,
                target = poisonProperties.ValueRO.targetFaction,
            });
            // //Adiciona o tempo do começo do PoisonDps(tick)
            var newPoisonDpsTick = currentTick;
            newPoisonDpsTick.Add(poisonProperties.ValueRO.dmgInterval);
            poisonDpsBuff.AddCommandData(new PoisonDps
            {
                Tick = currentTick,
                value = newPoisonDpsTick,
                damagePerTick = poisonProperties.ValueRO.damagePerTick,
                dmgInterval = poisonProperties.ValueRO.dmgInterval,
                // areaDamage = poisonProperties.ValueRO.areaDamage,

            });
            // Debug.Log(entity);
            toDestroy.Add(entity);
            // ECB.AddComponent<DestroyEntityTag>(entity);
        }
        //calcula o tempo da poison e dps duration 
        foreach (var (poisonDurationBuff, poisonDpsBuff, alreadyDamagedBuff, hitPos, entity) in SystemAPI.Query<DynamicBuffer<PoisonDuration>, DynamicBuffer<PoisonDps>, DynamicBuffer<AlreadyDamagedEntity>, RefRO<LocalTransform>>().WithEntityAccess())
        {

            // Verifica se o poison já expirou
            if (!poisonDurationBuff.GetDataAtTick(currentTick, out var poisonDurationElemnt))
            {
                poisonDurationElemnt.value = NetworkTick.Invalid;
            }
            bool endPoisonDuration = !poisonDurationElemnt.value.IsValid || currentTick.IsNewerThan(poisonDurationElemnt.value);
            if (endPoisonDuration)
            {
                ECB.DestroyEntity(entity);
            }
            // verifica se a duração das curses não acabou
            if (!poisonDpsBuff.GetDataAtTick(currentTick, out var poisonDpsElement))
            {
                poisonDpsElement.value = NetworkTick.Invalid;
            }

            bool dealDamage = !poisonDpsElement.value.IsValid || currentTick.IsNewerThan(poisonDpsElement.value);
            // var ArrowEffectsLookup = SystemAPI.GetComponentLookup<CurseStackEffect>();
            if (dealDamage)
            {
                NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
                var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;
                //cria uma esfera para achar entidades no entorno
                CollisionFilter collisionFilter = new CollisionFilter
                {
                    BelongsTo = ~0u,
                    CollidesWith = 1u << GameAssets.UNIT_LAYER,
                    GroupIndex = 0
                    // CollidesWith = (1u << GameAssets.PLAYER_LAYER) | (1u << GameAssets.ENEMY_LAYER),
                };
                // {

                if (collisionWorld.OverlapSphere(hitPos.ValueRO.Position, poisonDurationElemnt.radius, ref hits, collisionFilter))
                {
                    foreach (var hit in hits)
                    {
                        Entity hitEntity = hit.Entity;
                        if (SystemAPI.HasComponent<Team>(hitEntity))
                        {
                            var team = SystemAPI.GetComponentRO<Team>(hitEntity);

                            if (SystemAPI.HasComponent<CurrentHealth>(hitEntity))
                            {
                                if (team.ValueRO.faction == poisonDurationElemnt.target)
                                {
                                    var health = SystemAPI.GetComponentRW<CurrentHealth>(hitEntity);
                                    health.ValueRW.value -= (int)(poisonDpsElement.damagePerTick);
                                }
                            }
                        }
                    }
                }
                hits.Dispose();
                // }
                //Adiciona o tempo do começo do PoisonDps(tick)
                var newPoisonDpsTick = currentTick;
                newPoisonDpsTick.Add(poisonDpsElement.dmgInterval);
                poisonDpsBuff.Add(new PoisonDps
                {
                    Tick = currentTick,
                    value = newPoisonDpsTick,
                    damagePerTick = poisonDpsElement.damagePerTick,
                    dmgInterval = poisonDpsElement.dmgInterval,

                });
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
            // var lightningDurationLookup = SystemAPI.GetBufferLookup<LightningDuration>();
            // var lightningDpsLookup = SystemAPI.GetBufferLookup<LightningDps>();
            // var lightningDurationBuff = new DynamicBuffer<LightningDuration>();
            // var lightningDpsBuff = new DynamicBuffer<LightningDps>();
            var lightningVisual = new EffectUnitVisual();

            //adiciona os stacks de lightning
            ECB.AddComponent(target.ValueRO.Value, new LightningChain
            {
                radius = lightningProperties.ValueRO.radius,
                chainCount = lightningProperties.ValueRO.chainCount,
                target = lightningProperties.ValueRO.target,
                damage = lightningProperties.ValueRO.damage,
            });
            var lightningChainBuff = ECB.AddBuffer<LightningChainInfo>(target.ValueRO.Value);

            // lightning visual effect
            var visualUnitLightningSinalization = ECB.Instantiate(lightningProperties.ValueRO.sinalizationInstantiateParticle);
            lightningVisual.particleUnitEffect = visualUnitLightningSinalization;
            ECB.AddComponent(target.ValueRO.Value, lightningVisual);

            toDestroy.Add(entity);
            // ECB.DestroyEntity(entity);
        }
        foreach (var (lightningChain, lightningChainBuff, position, visualEffect, entity) in SystemAPI.Query<RefRW<LightningChain>, DynamicBuffer<LightningChainInfo>, RefRO<LocalTransform>, RefRO<EffectUnitVisual>>().WithEntityAccess())
        {
            var collisionWorld = SystemAPI.GetSingleton<PhysicsWorldSingleton>().CollisionWorld;

            // 1. Iniciamos a posição de busca a partir da origem do raio
            float3 currentSourcePos = position.ValueRO.Position;

            CollisionFilter filter = new CollisionFilter
            {
                BelongsTo = ~0u,
                CollidesWith = 1u << GameAssets.UNIT_LAYER,
                GroupIndex = 0
            };

            // 2. Criamos a lista de hits FORA do loop de saltos para reuso (Performance)
            NativeList<DistanceHit> hits = new NativeList<DistanceHit>(Allocator.Temp);
            //adiciona o primeiroinimigo atingido
            lightningChainBuff.Add(new LightningChainInfo
            {
                target = entity,
                position = position.ValueRO.Position
            });
            // Loop de saltos (Chain Count)
            for (int i = 0; i < lightningChain.ValueRO.chainCount; i++)
            {
                hits.Clear();
                float closestDist = lightningChain.ValueRO.radius;
                Entity bestTarget = Entity.Null;
                float3 bestPos = float3.zero;


                if (collisionWorld.OverlapSphere(currentSourcePos, lightningChain.ValueRO.radius, ref hits, filter))
                {
                    foreach (var hit in hits)
                    {
                        Entity hitEntity = hit.Entity;

                        // Validações básicas
                        if (hitEntity == entity || !SystemAPI.HasComponent<CurrentHealth>(hitEntity)) continue;

                        // Validação de Time/Facção
                        var team = SystemAPI.GetComponentRO<Team>(hitEntity);
                        if (team.ValueRO.faction != lightningChain.ValueRO.target) continue;

                        // CRÍTICO: Verifica se este inimigo já foi atingido NESTA corrente
                        if (IsAlreadyInChain(lightningChainBuff, hitEntity)) continue;

                        // Busca o mais próximo da posição atual da corrente
                        float dist = math.distance(currentSourcePos, hit.Position);
                        if (dist < closestDist)
                        {
                            closestDist = dist;
                            bestTarget = hitEntity;
                            bestPos = hit.Position;
                        }
                    }
                }

                // Se achou um alvo válido, adiciona à corrente
                if (bestTarget != Entity.Null)
                {
                    // Adicionamos diretamente ao buffer (sem ECB aqui) para leitura imediata no próximo salto
                    lightningChainBuff.Add(new LightningChainInfo
                    {
                        target = bestTarget,
                        position = bestPos
                    });

                    // O próximo salto sairá da posição DESTE inimigo
                    currentSourcePos = bestPos;
                }
                else
                {
                    break; // Não há mais alvos no alcance, encerra a corrente
                }
            }

            // 3. Aplica o dano a todos os atingidos salvos no buffer
            foreach (var chainInfo in lightningChainBuff)
            {
                var health = SystemAPI.GetComponentRW<CurrentHealth>(chainInfo.target);
                health.ValueRW.value -= (int)lightningChain.ValueRO.damage;
                ECB.AppendToBuffer(visualEffect.ValueRO.particleUnitEffect, chainInfo);
            }
            // Limpeza: Remove componentes para não processar novamente
            ECB.RemoveComponent<LightningChain>(entity);
            ECB.RemoveComponent<LightningChainInfo>(entity);
            // ECB.DestroyEntity(visualEffect.ValueRO.particleUnitEffect);
            ECB.AddComponent<DestroyEntityTag>(visualEffect.ValueRO.particleUnitEffect);

            hits.Dispose();
        }
        //ponto que vai destruir os efeitos
        for (int i = 0; i < toDestroy.Length; i++)
            ECB.DestroyEntity(toDestroy[i]);

        toDestroy.Dispose();
    }
    // Função auxiliar para evitar repetição de alvos
    private bool IsAlreadyInChain(DynamicBuffer<LightningChainInfo> buffer, Entity e)
    {
        for (int i = 0; i < buffer.Length; i++)
            if (buffer[i].target == e) return true;
        return false;
    }
}
