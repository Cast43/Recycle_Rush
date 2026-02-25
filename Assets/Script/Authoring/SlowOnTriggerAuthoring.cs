using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class SlowOnTriggerAuthoring : MonoBehaviour
{
    public float slowAmount;
    public float radius;
    public float duration;
    public Faction faction;

    public class Baker : Baker<SlowOnTriggerAuthoring>
    {
        public override void Bake(SlowOnTriggerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AreaSlow
            {
                slowAmount = (authoring.slowAmount),
                duration = authoring.duration
            });

            // AddComponent(entity, new DestroyOnTimer { value = authoring.duration });
            AddComponent(entity, new Team
            {
                faction = authoring.faction,
            });
            AddComponent(entity, new DurationAreaSlow { });
            AddBuffer<AlreadyDamagedEntity>(entity);
        }
    }
}