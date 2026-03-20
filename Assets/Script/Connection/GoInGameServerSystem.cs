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
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<ReceiveRpcCommandRequest>();
        EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<GoInGameRequestRpc>().WithAll<ReceiveRpcCommandRequest>();
        state.RequireForUpdate(state.GetEntityQuery(entityQueryBuilder));
        entityQueryBuilder.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        foreach ((RefRO<ReceiveRpcCommandRequest> request, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<GoInGameRequestRpc>().WithEntityAccess())
        {
            // Marca a conexão como "In Game" (logicamente presente)
            ecb.AddComponent<NetworkStreamInGame>(request.ValueRO.SourceConnection);

            // Adiciona a nossa nova Tag para dizer "este cara precisa de um corpo quando o jogo começar"
            ecb.AddComponent<WaitingToSpawnTag>(request.ValueRO.SourceConnection);

            ecb.DestroyEntity(entity);
        }
        ecb.Playback(state.EntityManager);
    }
}