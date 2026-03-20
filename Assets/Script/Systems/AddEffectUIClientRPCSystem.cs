using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
partial struct AddEffectUIClientRPCSystem : ISystem
{
    // [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp).WithAll<ShowUpgradesRPC>().WithAll<ReceiveRpcCommandRequest>();
        state.RequireForUpdate(state.GetEntityQuery(entityQueryBuilder));
        entityQueryBuilder.Dispose();
    }
    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {

        if (AddUpgradesUIManager.Instance == null)
        {
            // Só entra aqui se a UI realmente não existir na cena.
            return;
        }
        EntityCommandBuffer ECB = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        foreach ((RefRO<ReceiveRpcCommandRequest> receiveRpcCommandRequest, RefRO<ShowUpgradesRPC> showUpgrades, Entity entity)
            in SystemAPI.Query<RefRO<ReceiveRpcCommandRequest>, RefRO<ShowUpgradesRPC>>().WithEntityAccess())
        {
            // Aqui você pode ativar o GameObject
            var canvas = GameObject.Find("Canvas");
            if (canvas != null)
            {
                // Busca o filho "AddEffectUI" dentro do Canvas
                var addUpgradeUITransform = canvas.transform.Find("AddUpgradesUI");
                if (addUpgradeUITransform != null && !addUpgradeUITransform.gameObject.activeSelf)
                {
                    addUpgradeUITransform.gameObject.SetActive(true);
                    addUpgradeUITransform.GetComponent<AddUpgradesUIManager>().ShowUpgrades(showUpgrades.ValueRO.upgradeLevel);
                    // Debug.Log("AddEffectUI ativado");
                }
                else
                {
                    Debug.LogWarning("AddUpgradesUI não encontrado dentro do Canvas!");
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
            var addUpgradeUITransform = canvas.transform.Find("AddUpgradesUI");
            var upgradeOneUI = addUpgradeUITransform.transform.Find("UpgradeButton1");
            var upgradeTwoUI = addUpgradeUITransform.transform.Find("UpgradeButton2");
            var upgradeThreeIU = addUpgradeUITransform.transform.Find("UpgradeButton3");

            if (canvas == null) Debug.LogWarning("Canvas não encontrado!");
            if (addUpgradeUITransform == null) Debug.LogWarning("AddUpgradesUI não encontrado");
            if (upgradeOneUI == null) Debug.LogWarning("Upgrade 1 não encontrado");
            if (upgradeTwoUI == null) Debug.LogWarning("Upgrade 2 não encontrado");
            if (upgradeThreeIU == null) Debug.LogWarning("Upgrade 3 não encontrado");


        }

        ECB.Playback(state.EntityManager);
    }
}
