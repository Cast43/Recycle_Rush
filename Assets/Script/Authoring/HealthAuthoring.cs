using Unity.Entities;
using UnityEngine;

public class HealthAuthoring : MonoBehaviour
{
    public int MaxLife;

    public class Baker : Baker<HealthAuthoring>
    {
        public override void Bake(HealthAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CurrentHealth
            {
                value = authoring.MaxLife,
                onHealthChanged = true
            });
            AddComponent(entity, new MaxHealth { value = authoring.MaxLife });
            AddBuffer<DamageBufferElement>(entity);
            AddBuffer<DamageThisTick>(entity);

            // AddComponent(entity, new CurseStackEffect { value = 0, maxStack = 3 });
            // AddBuffer<CurseDuration>(entity);

        }
    }
}
