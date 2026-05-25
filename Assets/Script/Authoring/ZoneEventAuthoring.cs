using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class ZoneEventAuthoring : MonoBehaviour
{
    public float targetValue = 0;
    public float radius = 5f; // Adiciona o raio do evento
    public float timeLimit = 120f; // Adiciona o tempo limite de duração global do evento

    public class Baker : Baker<ZoneEventAuthoring>
    {
        public override void Bake(ZoneEventAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EventActiveTag { });
            
            AddComponent(entity, new EventObjective
            {
                Type = EventType.Cleanup,
                Progress = 0,
                TargetValue = authoring.targetValue,
                TimeLimit = authoring.timeLimit,
                TimeRemaining = authoring.timeLimit
            });
            
            AddComponent(entity, new EventAreaRadius
            {
                value = authoring.radius
            });
        }
    }
}