// ClientDamageUISystem.cs
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))] // Roda no final do frame
public partial class ClientDamageUISystem : SystemBase
{
    public void OnCreate(ref SystemState state)
    {
        //precisa dessa merda pra funcionar o rpc
        state.RequireForUpdate<ReceiveRpcCommandRequest>();
    }
    protected override void OnUpdate()
    {
        // Se a UI ainda não carregou, ignora
        if (WorldTextManager.Instance == null) return;

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);

        // Procura por RPCs que acabaram de chegar da rede
        foreach (var (rpcData, entity) in SystemAPI.Query<RefRO<DamageNumberRpc>>().WithAll<ReceiveRpcCommandRequest>().WithEntityAccess())
        {
            // Manda o MonoBehaviour instanciar o texto no Canvas World Space
            WorldTextManager.Instance.ShowDamage(rpcData.ValueRO.Position, rpcData.ValueRO.DamageAmount);

            // Destrói o RPC para não mostrar o mesmo dano no frame seguinte
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(EntityManager);
        ecb.Dispose();
    }
}