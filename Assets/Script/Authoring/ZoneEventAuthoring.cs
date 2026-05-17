using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class ZoneEventAuthoring : MonoBehaviour
{
    public float maxTime = 100;
    public float radius = 5f; // Adiciona o raio do evento

    public class Baker : Baker<ZoneEventAuthoring>
    {
        public override void Bake(ZoneEventAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EventPendingTag { });
            AddComponent(entity, new EventObjective
            {
                Type = EventType.Cleanup,
                Progress = 0,
                TargetValue = authoring.maxTime,
            });
            
            AddComponent(entity, new EventAreaRadius
            {
                value = authoring.radius
            });
        }
    }
}