using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct AddEffectComponentSystem : ISystem
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
                foreach (var effect in SystemAPI.Query<DynamicBuffer<GlobalEffectPrefab>>())
                {
                    for (int i = 0; i < effect.Length; i++)
                    {
                        if (recievedEffect.EffectName == effect[i].name)
                        {
                            //preciso achar o player local para adicionar o efeito
                            foreach (var (effectsPrefab, connection, localPlayer) in SystemAPI.Query<DynamicBuffer<EffectPrefab>, RefRO<ConnectionEntity>>().WithEntityAccess())
                            {
                                //encontra o player que enviou a conexao
                                if (connection.ValueRO.Value == receiveRpcCommandRequest.ValueRO.SourceConnection)
                                {
                                    // Debug.Log($"adicionou o efeito {effect[i].Prefab}");
                                    ECB.AppendToBuffer<EffectPrefab>(localPlayer, new EffectPrefab { Prefab = effect[i].Prefab });

                                }
                            }
                        }
                    }
                }
                // Destruir entidade após processar
                ECB.DestroyEntity(rpcEntity);
            }
        }
        ECB.Playback(state.EntityManager);

    }
}
