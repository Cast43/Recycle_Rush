using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameExperienceSystem))]
partial struct ApplyExperienceSystem : ISystem
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
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (currentExperience, maxExperience, level, buffUpgradesPending, getExperienceThisTickBuffer, entity) in
            SystemAPI.Query<RefRW<CurrentExperience>, RefRW<MaxExperience>, RefRW<Level>, DynamicBuffer<UpgradesPending>
            , DynamicBuffer<GetExperienceThisTick>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (!getExperienceThisTickBuffer.GetDataAtTick(currentTick, out var getExperienceThisTick)) continue;
            if (getExperienceThisTick.Tick != currentTick) continue;
            currentExperience.ValueRW.value += getExperienceThisTick.value;

            if (currentExperience.ValueRO.value >= maxExperience.ValueRO.value)
            {
                level.ValueRW.current++;
                currentExperience.ValueRW.value = (int)(currentExperience.ValueRO.value - maxExperience.ValueRO.value);
                maxExperience.ValueRW.value = (int)(maxExperience.ValueRO.modier * maxExperience.ValueRW.value);
            }
        }

        foreach (var (level, entity) in SystemAPI.Query<RefRW<Level>>().WithEntityAccess())
        {
            if (level.ValueRO.current > level.ValueRO.previous)
            {
                level.ValueRW.previous = level.ValueRO.current;
                ECB.AddComponent<LevelUpTag>(entity);
                // ECB.AddComponent<RequestChooseUpgrade>(entity);
                ECB.AppendToBuffer<UpgradesPending>(entity, new UpgradesPending
                {
                    upgradeLevel = UpgradeLevel.Commum
                });
            }
        }

        //passou de level
        //escolhe o efeito
        foreach (var (level, bufferLevelModifier, entity) in SystemAPI.Query<RefRO<Level>, DynamicBuffer<LevelModifier>>().WithAll<LevelUpTag>().WithEntityAccess())
        {
            int lvl = level.ValueRO.current;
            // Apply each modifier according to current level
            foreach (var mod in bufferLevelModifier)
            {
                switch (mod.Type)
                {
                    case UpgradeModifier.IncreaseHealth:
                        if (state.EntityManager.HasComponent<MaxHealth>(entity))
                        {
                            var maxHealth = state.EntityManager.GetComponentData<MaxHealth>(entity);
                            maxHealth.value += (int)mod.Value * lvl;
                            ECB.SetComponent(entity, maxHealth);
                        }
                        if (state.EntityManager.HasComponent<Enemy>(entity))
                        {
                            var maxHealth = state.EntityManager.GetComponentData<MaxHealth>(entity);
                            var currentHealth = state.EntityManager.GetComponentData<CurrentHealth>(entity);
                            maxHealth.value += (int)(((int)mod.Value * lvl) / mod.divideWaveGain);
                            currentHealth.value = maxHealth.value;
                            ECB.SetComponent(entity, maxHealth);
                            ECB.SetComponent(entity, currentHealth);
                        }
                        break;
                    case UpgradeModifier.IncreaseDamage:
                        if (state.EntityManager.HasComponent<MeleeAttackProperties>(entity))
                        {
                            var meleeAttack = state.EntityManager.GetComponentData<MeleeAttackProperties>(entity);
                            meleeAttack.damage = (int)(meleeAttack.damage + ((mod.Value * lvl) / mod.divideWaveGain));
                            ECB.SetComponent(entity, meleeAttack);
                        }
                        break;
                    case UpgradeModifier.DecreaseShootTime:
                        if (state.EntityManager.HasComponent<ShootAttackProperties>(entity))
                        {
                            var shootAttack = state.EntityManager.GetComponentData<ShootAttackProperties>(entity);
                            shootAttack.cooldownTickCount = (uint)(shootAttack.cooldownTickCount / ((mod.Value * lvl) + 1));
                            ECB.SetComponent(entity, shootAttack);
                        }
                        break;
                    case UpgradeModifier.IncreaseSpeed:
                        if (state.EntityManager.HasComponent<PlayerInput>(entity))
                        {
                            var ms = state.EntityManager.GetComponentData<MoveSpeed>(entity);
                            ms.maxSpeed += mod.Value * lvl;
                            ms.currentSpeed += mod.Value * lvl;
                            ECB.SetComponent(entity, ms);
                        }
                        if (state.EntityManager.HasComponent<Movement>(entity))
                        {
                            var ms = state.EntityManager.GetComponentData<MoveSpeed>(entity);
                            ms.maxSpeed += mod.Value * lvl;
                            ms.currentSpeed += mod.Value * lvl;
                            ECB.SetComponent(entity, ms);
                        }
                        break;
                }
            }

            ECB.RemoveComponent<LevelUpTag>(entity);
        }

        // foreach (var (buffUpgradesPending, entity) in SystemAPI.Query<DynamicBuffer<UpgradesPending>>().WithAll<PlayerInput>().WithEntityAccess())
        // {

        //     // manda um rpc para o player escolher os efeitos a adição de efeitos
        //     // Enviar RPC para esse cliente
        //     //arrumar outro método pra achar o mundo do cliente esse está dando bug na build
        //     // Pega o NetworkId do cliente local
        //     //essa parte não é do inimigo apenas do player

        //     if (buffUpgradesPending.IsEmpty)
        //     {
        //         //     ECB.RemoveComponent<RequestChooseUpgrade>(entity);
        //         continue;
        //     }

        //     var lookupConnection = SystemAPI.GetComponentLookup<ConnectionEntity>(true);
        //     if (!lookupConnection.HasComponent(entity))
        //         continue; // sem conexão vinculada a essa entidade, pula

        //     var connection = lookupConnection[entity]; // ConnectionEntity { Value = connectionEntity }

        //     // pega o NetworkId da entidade de conexão (server tem vários NetworkId)
        //     var networkIdLookup = SystemAPI.GetComponentLookup<NetworkId>(true);
        //     if (!networkIdLookup.HasComponent(connection.Value))
        //         continue; // conexão sem NetworkId? pula

        //     var netId = networkIdLookup[connection.Value];

        //     // cria RPC e envia para a conexão correta
        //     var rpc = new ShowUpgradesRPC();

        //     if (buffUpgradesPending[0].upgradeLevel == UpgradeLevel.Commum)
        //     {
        //         rpc = new ShowUpgradesRPC
        //         {
        //             ClientNetId = netId.Value,
        //             upgradeLevel = UpgradeLevel.Commum
        //         };
        //     }
        //     else
        //     {
        //         rpc = new ShowUpgradesRPC
        //         {
        //             ClientNetId = netId.Value,
        //             upgradeLevel = UpgradeLevel.Core
        //         };
        //     }


        //     var rpcEntity = ECB.CreateEntity();
        //     ECB.AddComponent(rpcEntity, rpc);
        //     ECB.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = connection.Value });
        // }

        ECB.Playback(state.EntityManager);
        ECB.Dispose();
    }
}
