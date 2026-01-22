using Unity.Entities;
using UnityEngine;

public class HealthAuthoring : MonoBehaviour
{
    public int MaxLife;
    public float healthRegenCooldown = 1;
    public int regenAmount = 5;

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
            AddComponent(entity, new HealthRegen
            {
                amount = authoring.regenAmount,
                cooldownRestore = (uint)authoring.healthRegenCooldown
            });

            AddBuffer<DamageBufferElement>(entity);
            AddBuffer<DamageThisTick>(entity);
            AddBuffer<HealthRegenCooldown>(entity);

            // AddComponent(entity, new CurseStackEffect { value = 0, maxStack = 3 });
            // AddBuffer<CurseDuration>(entity);

        }
    }
}
