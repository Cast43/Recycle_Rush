using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ReceivePlayerReadySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        // Só roda se houver pedidos de RPC para ler
        state.RequireForUpdate<ReceiveRpcCommandRequest>();
        var query = SystemAPI.QueryBuilder().WithAll<PlayerReadyRpc, ReceiveRpcCommandRequest>().Build();
        state.RequireForUpdate(query);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (request, entity) in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<PlayerReadyRpc>().WithEntityAccess())
        {
            // A "SourceConnection" é a entidade que representa o jogador na rede.
            // Adicionamos a tag nela para saber que ESTE jogador específico está pronto.
            ecb.AddComponent<PlayerReadyTag>(request.ValueRO.SourceConnection);

            // Destrói a mensagem do RPC para não processar duas vezes
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
    }
}