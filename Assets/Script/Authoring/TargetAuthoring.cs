using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class TargetAuthoring : MonoBehaviour
{

    public Faction targetFaction;
    public float targetRadius;
    public class Baker : Baker<TargetAuthoring>
    {
        public override void Bake(TargetAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new TargetRadius { value = authoring.targetRadius });
            AddComponent(entity, new TargetFind { value = authoring.targetFaction });
            AddComponent<TargetEntity>(entity);
        }
    }
}
