using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class DamageOnTriggerAuthoring : MonoBehaviour
{
    public int damageOnTrigger;
    public class Baker : Baker<DamageOnTriggerAuthoring>
    {
        public override void Bake(DamageOnTriggerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DamageOnTrigger { value = authoring.damageOnTrigger });
            AddBuffer<AlreadyDamagedEntity>(entity);
        }
    }
}