using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
partial struct HealthBarSystem : ISystem
{
    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        Vector3 cameraForward = Vector3.zero;
        if (Camera.main != null)
        {
            cameraForward = Camera.main.transform.forward;
        }

        foreach ((RefRW<LocalTransform> localTransform, RefRO<HealthBar> healthBar)
                in SystemAPI.Query<RefRW<LocalTransform>, RefRO<HealthBar>>())
        {
            LocalTransform parentLocalTransform = SystemAPI.GetComponent<LocalTransform>(healthBar.ValueRO.healthEntity);

            CurrentHealth curHealth = SystemAPI.GetComponent<CurrentHealth>(healthBar.ValueRO.healthEntity);
            MaxHealth maxHealth = SystemAPI.GetComponent<MaxHealth>(healthBar.ValueRO.healthEntity);
            // Debug.Log($"[HealthBarSystem] {healthBar.ValueRO.healthEntity} onHealthChanged: {curHealth.onHealthChanged} scale: {localTransform.ValueRO.Scale}");
            // if (!curHealth.onHealthChanged)
            // {
            //     // Debug.Log("test" + healthBar.ValueRO.healthEntity);
            //     continue;
            // }

            // Após atualizar, reseta o flag:
            float healthNormalized = (float)curHealth.value / maxHealth.value;
            // if (healthNormalized == 1f)
            // {
            //     localTransform.ValueRW.Scale = 0;
            // }
            // else
            // {
            //     localTransform.ValueRW.Scale = 1;
            // }

            RefRW<PostTransformMatrix> barVisualPostTransformMatrix = SystemAPI.GetComponentRW<PostTransformMatrix>(healthBar.ValueRO.barVisualEntity);
            barVisualPostTransformMatrix.ValueRW.Value = float4x4.Scale(healthNormalized, 0.3f, 1);
        }
    }
}
