using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EventsAuthoring : MonoBehaviour
{
    public EventType eventType = EventType.Cleanup;
    public float targetValue = 10f;
    public float timeLimit = 120f;
    public float eventRadius = 15f;

    public class Baker : Baker<EventsAuthoring>
    {
        public override void Bake(EventsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EventActiveTag { });
            AddComponent(entity, new EventObjective
            {
                Type = authoring.eventType,
                Progress = 0,
                TargetValue = authoring.targetValue,
                TimeLimit = authoring.timeLimit,
                TimeRemaining = authoring.timeLimit
            });
            AddComponent(entity, new EventAreaRadius { value = authoring.eventRadius });
        }
    }
}