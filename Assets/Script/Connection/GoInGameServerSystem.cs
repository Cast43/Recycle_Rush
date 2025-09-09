using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Rendering;
using UnityEngine;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
partial struct GoInGameServerSystem : ISystem
{
    public static int playersCount;
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        playersCount = 0;
        state.RequireForUpdate<EntitiesReferences>();
        //precisa dessa merda pra funcionar o rpc
        state.RequireForUpdate<ReceiveRpcCommandRequest>();
        state.RequireForUpdate<NetworkId>();
        EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<GoInGameRequestRpc>().WithAll<ReceiveRpcCommandRequest>();
        state.RequireForUpdate(state.GetEntityQuery(entityQueryBuilder));
        entityQueryBuilder.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        EntityCommandBuffer entityCommandBuffer = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRpc>().WithEntityAccess())
        {
            entityCommandBuffer.AddComponent<NetworkStreamInGame>(receiveRpcCommandRequest.ValueRO.SourceConnection);
            // Debug.Log("Client connected to Server");
            entityCommandBuffer.DestroyEntity(entity);

            Entity playerObjectEntity = Entity.Null;

            if (playersCount == 0)
            {
                playerObjectEntity = entityCommandBuffer.Instantiate(entitiesReferences.playerRPrefab);
            }
            else if (playersCount == 1)
            {
                playerObjectEntity = entityCommandBuffer.Instantiate(entitiesReferences.playerBPrefab);
            }
            else if (playersCount == 2)
            {
                playerObjectEntity = entityCommandBuffer.Instantiate(entitiesReferences.playerYPrefab);
            }
            else
            {
                playerObjectEntity = entityCommandBuffer.Instantiate(entitiesReferences.playerGPrefab);
            }
            // Debug.Log(playersCount);
            playersCount++;
            //spawn Player
            // Entity playerObjectEntity = entityCommandBuffer.Instantiate(entitiesReferences.playerRPrefab);
            entityCommandBuffer.SetComponent(playerObjectEntity, LocalTransform.FromPosition(new float3(UnityEngine.Random.Range(-10, +10), 1, 0)));

            //atribui o id do player ao gameobject do player
            NetworkId networkId = SystemAPI.GetComponent<NetworkId>(receiveRpcCommandRequest.ValueRO.SourceConnection);

            //adiciona o componente de owner ao player
            entityCommandBuffer.AddComponent(playerObjectEntity, new GhostOwner
            {
                //atribui o valor do id do player
                NetworkId = networkId.Value,
            });

            entityCommandBuffer.AddComponent(playerObjectEntity, new ConnectionEntity
            {
                //atribui o valor do id do player
                Value = receiveRpcCommandRequest.ValueRO.SourceConnection,
            });
            //faz com que o player seja excluido se desconectado adicionando ele no link group
            entityCommandBuffer.AppendToBuffer(receiveRpcCommandRequest.ValueRO.SourceConnection, new LinkedEntityGroup
            {
                Value = playerObjectEntity,
            });
        }
        entityCommandBuffer.Playback(state.EntityManager);
    }
}
