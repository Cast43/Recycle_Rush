using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct AddUpgradeSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        //precisa dessa merda pra funcionar o rpc
        state.RequireForUpdate<ReceiveRpcCommandRequest>();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ECB = new EntityCommandBuffer(Allocator.Temp);

        // Lookups DEVEM ficar fora das queries para segurança de memória e não quebrar o ECS
        var statusModifierLookup = SystemAPI.GetBufferLookup<StatusModifier>(true);
        var techLookup = SystemAPI.GetBufferLookup<Tech>(true);
        var networkIdLookup = SystemAPI.GetComponentLookup<NetworkId>(true);

        // ==========================================
        // 1. SISTEMA PARA ADICIONAR EFEITOS
        // ==========================================
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity rpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<AddEffectRpc>().WithEntityAccess())
        {
            var recievedEffect = SystemAPI.GetComponent<AddEffectRpc>(rpcEntity);

            int sourceNetId = -1;
            if (networkIdLookup.HasComponent(receiveRpcCommandRequest.ValueRO.SourceConnection))
            {
                sourceNetId = networkIdLookup[receiveRpcCommandRequest.ValueRO.SourceConnection].Value;
            }

            foreach (var upgrades in SystemAPI.Query<DynamicBuffer<GlobalUpgradesPrefab>>())
            {
                for (int i = 0; i < upgrades.Length; i++)
                {
                    if (recievedEffect.EffectName == upgrades[i].Name)
                    {
                        foreach (var (ghostOwner, localPlayer) in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<PlayerInput>().WithEntityAccess())
                        {
                            if (ghostOwner.ValueRO.NetworkId == sourceNetId)
                            {
                                var pendingBuffer = SystemAPI.GetBuffer<UpgradesPending>(localPlayer);
                                if (pendingBuffer.Length == 0) continue;

                                pendingBuffer.RemoveAt(0);
                                ECB.AppendToBuffer(localPlayer, new EffectPrefab { Prefab = upgrades[i].Prefab, name = upgrades[i].Name });
                            }
                        }
                    }
                }
            }
            ECB.DestroyEntity(rpcEntity);
        }

        // ==========================================
        // 2. SISTEMA PARA AUMENTAR O STATUS DO JOGADOR
        // ==========================================
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity rpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<ModifierStatusRpc>().WithEntityAccess())
        {
            var recievedStatus = SystemAPI.GetComponent<ModifierStatusRpc>(rpcEntity);

            int sourceNetId = -1;
            if (networkIdLookup.HasComponent(receiveRpcCommandRequest.ValueRO.SourceConnection))
            {
                sourceNetId = networkIdLookup[receiveRpcCommandRequest.ValueRO.SourceConnection].Value;
            }

            foreach (var upgrades in SystemAPI.Query<DynamicBuffer<GlobalUpgradesPrefab>>())
            {
                for (int i = 0; i < upgrades.Length; i++)
                {
                    if (recievedStatus.ModifierName == upgrades[i].Name)
                    {
                        foreach (var (ghostOwner, localPlayer) in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<PlayerInput>().WithEntityAccess())
                        {
                            if (ghostOwner.ValueRO.NetworkId == sourceNetId)
                            {
                                var pendingBuffer = SystemAPI.GetBuffer<UpgradesPending>(localPlayer);
                                if (pendingBuffer.Length == 0) continue;
                                
                                pendingBuffer.RemoveAt(0);

                                if (statusModifierLookup.HasBuffer(upgrades[i].Prefab))
                                {
                                    var statusModifier = statusModifierLookup[upgrades[i].Prefab];
                                    foreach (var status in statusModifier)
                                    {
                                        ECB.AppendToBuffer(localPlayer, new StatusModifier { Type = status.Type, Value = status.Value });
                                    }
                                }
                                ECB.AddComponent<UpdateStatus>(localPlayer);
                            }
                        }
                    }
                }
            }
            ECB.DestroyEntity(rpcEntity);
        }

        // ==========================================
        // 3. SISTEMA PARA ADICIONAR COMPONENTE (TECH)
        // ==========================================
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity rpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<AddComponentRpc>().WithEntityAccess())
        {
            var recievedPower = SystemAPI.GetComponent<AddComponentRpc>(rpcEntity);

            int sourceNetId = -1;
            if (networkIdLookup.HasComponent(receiveRpcCommandRequest.ValueRO.SourceConnection))
            {
                sourceNetId = networkIdLookup[receiveRpcCommandRequest.ValueRO.SourceConnection].Value;
            }

            foreach (var upgrades in SystemAPI.Query<DynamicBuffer<GlobalUpgradesPrefab>>())
            {
                for (int i = 0; i < upgrades.Length; i++)
                {
                    if (recievedPower.ComponentName == upgrades[i].Name)
                    {
                        foreach (var (ghostOwner, localPlayer) in SystemAPI.Query<RefRO<GhostOwner>>().WithAll<PlayerInput>().WithEntityAccess())
                        {
                            if (ghostOwner.ValueRO.NetworkId == sourceNetId)
                            {
                                var pendingBuffer = SystemAPI.GetBuffer<UpgradesPending>(localPlayer);
                                if (pendingBuffer.Length == 0) continue;
                                
                                pendingBuffer.RemoveAt(0);

                                if (techLookup.HasBuffer(upgrades[i].Prefab))
                                {
                                    var techBuffer = techLookup[upgrades[i].Prefab];
                                    foreach (var tech in techBuffer)
                                    {
                                        ECB.AppendToBuffer(localPlayer, new Tech
                                        {
                                            Type = tech.Type,
                                            amount = tech.amount,
                                            modifier = tech.modifier,
                                            distance = tech.distance,
                                            maxDistance = tech.maxDistance,
                                            cooldown = tech.cooldown
                                        });
                                    }
                                }
                                ECB.AddComponent<AddTech>(localPlayer);
                            }
                        }
                    }
                }
            }
            ECB.DestroyEntity(rpcEntity);
        }

        ECB.Playback(state.EntityManager);
    }
}
