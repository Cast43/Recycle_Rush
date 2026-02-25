using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class DamageAreaAuthoring : MonoBehaviour
{
    public float duration;
    public int dmgPerTick;
    public float timeToDmg;
    public float dmgInterval;
    public float radius;
    public Faction targetFaction;
    public GameObject start;
    public GameObject middle;
    public GameObject end;

    public class Baker : Baker<DamageAreaAuthoring>
    {
        public override void Bake(DamageAreaAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DestroyOnTimer { value = authoring.duration });
            AddComponent(entity, new Team { faction = authoring.targetFaction });
            AddComponent(entity, new AreaVisualState { IsImpacting = false });
            AddComponent(entity, new AreaDamage
            {
                dmgPerTick = authoring.dmgPerTick,
                timeToDmg = authoring.timeToDmg,
                dmgInterval = authoring.dmgInterval,
                radius = authoring.radius,
                start = GetEntity(authoring.start, TransformUsageFlags.Dynamic),
                middle = GetEntity(authoring.middle, TransformUsageFlags.Dynamic),
                end = GetEntity(authoring.end, TransformUsageFlags.Dynamic)
            });
            AddComponent<AreaDamageTimer>(entity);
            AddBuffer<AlreadyDamagedEntity>(entity);
        }
    }
}
