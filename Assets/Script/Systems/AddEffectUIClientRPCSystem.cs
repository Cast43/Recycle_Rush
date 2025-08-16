using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct AddEffectUIClientRPCSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<ShowAddEffectRPC>().WithAll<ReceiveRpcCommandRequest>();
        state.RequireForUpdate(state.GetEntityQuery(entityQueryBuilder));
        entityQueryBuilder.Dispose();
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ECB = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>>().WithAll<ShowAddEffectRPC>().WithEntityAccess())
        {
            // Aqui você pode ativar o GameObject
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                // Busca o filho "AddEffectUI" dentro do Canvas
                var addEffectUITransform = canvas.transform.Find("AddEffectUI");
                if (addEffectUITransform != null)
                {
                    addEffectUITransform.gameObject.SetActive(true);
                    // Debug.Log("AddEffectUI ativado");
                }
                else
                {
                    Debug.LogWarning("AddEffectUI não encontrado dentro do Canvas!");
                }
            }
            else
            {
                Debug.LogWarning("Canvas não encontrado!");
            }
            // Remove o marcador para não repetir
            ECB.DestroyEntity(entity);
        }

        foreach ((RefRO<PlayerInput> player, DynamicBuffer<EffectPrefab> effects, Entity entity)
    in SystemAPI.Query<RefRO<PlayerInput>, DynamicBuffer<EffectPrefab>>().WithEntityAccess())
        {
            // Aqui você pode ativar o GameObject
            var canvas = GameObject.Find("Canvas");
            var addEffectUITransform = canvas.transform.Find("AddEffectUI");
            var effectOneUI = addEffectUITransform.transform.Find("EffectButton1");
            var effectTwoUI = addEffectUITransform.transform.Find("EffectButton2");
            var effectThreeIU = addEffectUITransform.transform.Find("EffectButton3");

            if (canvas == null) Debug.LogWarning("Canvas não encontrado!");
            if (addEffectUITransform == null) Debug.LogWarning("AddEffectUI não encontrado");
            if (effectOneUI == null) Debug.LogWarning("Efeito 1 não encontrado");
            if (effectTwoUI == null) Debug.LogWarning("Efeito 2 não encontrado");
            if (effectThreeIU == null) Debug.LogWarning("Efeito 3 não encontrado");


        }
        ECB.Playback(state.EntityManager);
    }

}
