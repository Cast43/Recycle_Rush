using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class PoisonEffectAreaAuthoring : MonoBehaviour
{
    // public float duration;
    // public float areaRadius;
    // public Faction targetFaction;
    // public int damagePerTick;
    // public float dmgInterval;
    // public bool areaDamage;

    public class Baker : Baker<PoisonEffectAreaAuthoring>
    {
        public override void Bake(PoisonEffectAreaAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PoisonVisualTag { });
            var poisonDurationBuff = AddBuffer<PoisonDuration>(entity);
            var poisonAlreadyDamage = AddBuffer<AlreadyDamagedEntity>(entity);
            var poisonDpsBuff = AddBuffer<PoisonDps>(entity);
        }
    }
}