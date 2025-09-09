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

        foreach (var (currentExperience, maxExperience, level, getExperienceThisTickBuffer, entity) in
            SystemAPI.Query<RefRW<CurrentExperience>, RefRO<MaxExperience>, RefRW<Level>
            , DynamicBuffer<GetExperienceThisTick>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (!getExperienceThisTickBuffer.GetDataAtTick(currentTick, out var getExperienceThisTick)) continue;
            if (getExperienceThisTick.Tick != currentTick) continue;
            currentExperience.ValueRW.value += getExperienceThisTick.value;

            if (currentExperience.ValueRO.value >= maxExperience.ValueRO.value)
            {
                level.ValueRW.current += (int)(currentExperience.ValueRO.value / maxExperience.ValueRO.value);
                currentExperience.ValueRW.value = (int)(currentExperience.ValueRO.value % maxExperience.ValueRO.value);
            }
        }

        foreach (var (level, entity) in SystemAPI.Query<RefRW<Level>>().WithEntityAccess())
        {
            if (level.ValueRO.current > level.ValueRO.previous)
            {
                level.ValueRW.previous = level.ValueRO.current;
                ECB.AddComponent<LevelUpTag>(entity);
                ECB.AddComponent<RequestChooseEffect>(entity);
            }
        }
        //passou de level
        foreach (var (level, connection, buffer, entity) in SystemAPI.Query<RefRO<Level>, RefRO<ConnectionEntity>, DynamicBuffer<LevelModifier>>().WithAll<LevelUpTag>().WithEntityAccess())
        {
            int lvl = level.ValueRO.current;
            // Apply each modifier according to current level
            foreach (var mod in buffer)
            {
                switch (mod.Type)
                {
                    case ModifierType.AddHealth:
                        if (state.EntityManager.HasComponent<MaxHealth>(entity))
                        {
                            var maxHealth = state.EntityManager.GetComponentData<MaxHealth>(entity);
                            maxHealth.value += (int)mod.Value * lvl;
                            ECB.SetComponent(entity, maxHealth);
                        }
                        break;
                    case ModifierType.IncreaseDamage:
                        if (state.EntityManager.HasComponent<MeleeAttackProperties>(entity))
                        {
                            var meleeAttack = state.EntityManager.GetComponentData<MeleeAttackProperties>(entity);
                            meleeAttack.damage = (int)(meleeAttack.damage / ((mod.Value * lvl) + 1));
                            ECB.SetComponent(entity, meleeAttack);
                        }
                        break;
                    case ModifierType.DecreaseShootTime:
                        if (state.EntityManager.HasComponent<ShootAttackProperties>(entity))
                        {
                            var shootAttack = state.EntityManager.GetComponentData<ShootAttackProperties>(entity);
                            shootAttack.cooldownTickCount = (uint)(shootAttack.cooldownTickCount / ((mod.Value * lvl) + 1));
                            ECB.SetComponent(entity, shootAttack);
                        }
                        break;
                    case ModifierType.IncreaseSpeed:
                        if (state.EntityManager.HasComponent<PlayerInput>(entity))
                        {
                            var ms = state.EntityManager.GetComponentData<MoveSpeed>(entity);
                            ms.value += mod.Value * lvl;
                            ECB.SetComponent(entity, ms);
                        }
                        if (state.EntityManager.HasComponent<Movement>(entity))
                        {
                            var ms = state.EntityManager.GetComponentData<MoveSpeed>(entity);
                            ms.value += mod.Value * lvl;
                            ECB.SetComponent(entity, ms);
                        }
                        break;
                }
            }

            // var networkIdLookup = SystemAPI.GetComponentLookup<NetworkId>();
            // var networkId = networkIdLookup[entity];

            // manda um rpc para o player escolher os efeitos a adição de efeitos
            // Enviar RPC para esse cliente
            //arrumar outro método pra achar o mundo do cliente esse está dando bug na build
            // Pega o NetworkId do cliente local
            NetworkId netId = SystemAPI.GetSingleton<NetworkId>();

            // Cria o RPC usando o NetworkId
            var rpc = new ShowAddEffectRPC { ClientNetId = netId.Value };
            // state.EntityManager.CreateEntity(typeof(ShowAddEffectRPC));
            var rpcEntity = ECB.CreateEntity();
            ECB.AddComponent(rpcEntity, rpc);
            ECB.AddComponent(rpcEntity, new SendRpcCommandRequest { TargetConnection = connection.ValueRO.Value });
            // Remove tag
            ECB.RemoveComponent<LevelUpTag>(entity);
        }

        ECB.Playback(state.EntityManager);
        ECB.Dispose();
    }
}
