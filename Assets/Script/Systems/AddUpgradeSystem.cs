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

        //pra cada rpc eu preciso achar a instancia do efeito global
        //verificar se o nome do efeito bate com algum dos efeitos globais
        //encontrar o effect prefab do player local e adicionar o efeito no buffer

        // foreach ((RefRO<ConnectionEntity> connectEntity, RefRO<AddEffectRpc> addEffect, Entity rpcEntity)
        // in SystemAPI.Query<RefRO<ConnectionEntity>, RefRO<AddEffectRpc>>().WithEntityAccess())
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity rpcEntity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<AddEffectRpc>().WithEntityAccess())
        {
            {
                // Debug.Log($"Servidor recebeu efeito: {receiveRpcCommandRequest.ValueRO.SourceConnection}");
                var recievedEffect = SystemAPI.GetComponent<AddEffectRpc>(rpcEntity);

                //passo por cada efeito global verificando se o nome do efeito bate com o nome enviado pelo RPC
                foreach (var upgrades in SystemAPI.Query<DynamicBuffer<GlobalUpgradesPrefab>>())
                {
                    for (int i = 0; i < upgrades.Length; i++)
                    {
                        if (recievedEffect.EffectName == upgrades[i].Name)
                        {
                            //preciso achar o player local para adicionar o efeito
                            foreach (var (effectsPrefab, connection, localPlayer) in SystemAPI.Query<DynamicBuffer<EffectPrefab>, RefRO<ConnectionEntity>>().WithEntityAccess())
                            {
                                //encontra o player que enviou a conexao
                                if (connection.ValueRO.Value == receiveRpcCommandRequest.ValueRO.SourceConnection)
                                {
                                    // Debug.Log($"adicionou o efeito {effect[i].Prefab}");
                                    ECB.AppendToBuffer<EffectPrefab>(localPlayer, new EffectPrefab { Prefab = upgrades[i].Prefab, name = upgrades[i].Name });

                                }
                            }
                        }
                    }
                }
                // Destruir entidade após processar
                ECB.DestroyEntity(rpcEntity);
            }
        }

        //sistema para aumentar o status do jogador
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity rpcEntity)
    in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<ModifierStatusRpc>().WithEntityAccess())
        {
            {
                // Debug.Log($"Servidor recebeu efeito: {receiveRpcCommandRequest.ValueRO.SourceConnection}");
                var recievedStatus = SystemAPI.GetComponent<ModifierStatusRpc>(rpcEntity);

                //passo por cada efeito global verificando se o nome do efeito bate com o nome enviado pelo RPC
                foreach (var upgrades in SystemAPI.Query<DynamicBuffer<GlobalUpgradesPrefab>>())
                {
                    for (int i = 0; i < upgrades.Length; i++)
                    {
                        if (recievedStatus.ModifierName == upgrades[i].Name)
                        {
                            //preciso achar o player local para adicionar o efeito
                            foreach (var (LevelModifier, connection, localPlayer) in SystemAPI.Query<DynamicBuffer<StatusModifier>, RefRO<ConnectionEntity>>().WithEntityAccess())
                            {
                                //encontra o player que enviou a conexao
                                if (connection.ValueRO.Value == receiveRpcCommandRequest.ValueRO.SourceConnection)
                                {
                                    // Debug.Log($"adicionou o efeito {effect[i].Prefab}");
                                    BufferLookup<StatusModifier> statusModifierBuffer = SystemAPI.GetBufferLookup<StatusModifier>();
                                    var statusModifier = statusModifierBuffer[upgrades[i].Prefab];
                                    foreach (var status in statusModifier)
                                    {
                                        ECB.AppendToBuffer<StatusModifier>(localPlayer, new StatusModifier { Type = status.Type, Value = status.Value });
                                    }
                                    ECB.AddComponent<UpdateStatus>(localPlayer);
                                }
                            }
                        }
                    }
                }
                // Destruir entidade após processar
                ECB.DestroyEntity(rpcEntity);
            }
        }

        //sistema para adicionar um componente ao jogador
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity rpcEntity)
    in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<AddComponentRpc>().WithEntityAccess())
        {
            {
                // Debug.Log($"Servidor recebeu efeito: {receiveRpcCommandRequest.ValueRO.SourceConnection}");
                var recievedPower = SystemAPI.GetComponent<AddComponentRpc>(rpcEntity);

                //passo por cada efeito global verificando se o nome do efeito bate com o nome enviado pelo RPC
                foreach (var upgrades in SystemAPI.Query<DynamicBuffer<GlobalUpgradesPrefab>>())
                {
                    for (int i = 0; i < upgrades.Length; i++)
                    {
                        if (recievedPower.ComponentName == upgrades[i].Name)
                        {
                            //preciso achar o player local para adicionar o efeito
                            foreach (var (addTech, connection, localPlayer) in SystemAPI.Query<DynamicBuffer<Tech>, RefRO<ConnectionEntity>>().WithEntityAccess())
                            {
                                //encontra o player que enviou a conexao
                                if (connection.ValueRO.Value == receiveRpcCommandRequest.ValueRO.SourceConnection)
                                {
                                    // Debug.Log($"adicionou o efeito {effect[i].Prefab}");
                                    BufferLookup<Tech> techBufferLookup = SystemAPI.GetBufferLookup<Tech>();
                                    var techBuffer = techBufferLookup[upgrades[i].Prefab];
                                    foreach (var tech in techBuffer)
                                    {
                                        ECB.AppendToBuffer<Tech>(localPlayer, new Tech
                                        {
                                            Type = tech.Type,
                                            amount = tech.amount,
                                            modifier = tech.modifier,
                                            distance = tech.distance,
                                            maxDistance = tech.maxDistance,
                                            cooldown = tech.cooldown
                                        });
                                    }
                                    ECB.AddComponent<AddTech>(localPlayer);
                                }
                            }
                        }
                    }
                }
                // Destruir entidade após processar
                ECB.DestroyEntity(rpcEntity);
            }

        }

        //sistema para remover o count dos upgrades pendentes 
        foreach (var (receiveRpcCommandRequest, rpcEntity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<DecreaseUpgradesPendingRpc>().WithEntityAccess())
        {
            //preciso achar o player local para remover um dos upgrades pending
            foreach (var (upgradesPending, connection, localPlayer) in SystemAPI.Query<DynamicBuffer<UpgradesPending>, RefRO<ConnectionEntity>>().WithEntityAccess())
            {
                //encontra o player que enviou a conexao
                if (connection.ValueRO.Value == receiveRpcCommandRequest.ValueRO.SourceConnection)
                {
                    var buffer = SystemAPI.GetBuffer<UpgradesPending>(localPlayer);
                    if (buffer.Length > 0) buffer.RemoveAt(0);
                }
            }
            ECB.DestroyEntity(rpcEntity);
        }

        ECB.Playback(state.EntityManager);

    }
}
