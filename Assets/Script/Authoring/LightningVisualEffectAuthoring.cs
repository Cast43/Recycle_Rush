using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class LightningVisualEffectAuthoring : MonoBehaviour
{

    public class Baker : Baker<LightningVisualEffectAuthoring>
    {
        public override void Bake(LightningVisualEffectAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new LightningVisualTag { });
            AddBuffer<LightningChainInfo>(entity);
        }
    }
}
