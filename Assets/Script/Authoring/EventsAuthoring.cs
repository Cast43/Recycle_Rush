using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EventsAuthoring : MonoBehaviour
{
    public int maxEnergy;
    public int startEnergy = 100;
    public float energyRestoreCooldown = 1;
    public int energyRestoreAmount = 5;

    public class Baker : Baker<EventsAuthoring>
    {
        public override void Bake(EventsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EventPendingTag { });
            AddComponent(entity, new EventObjective
            {
                Type = EventType.Cleanup,
                Progress = 0,
                TargetValue = 10
            });

            AddBuffer<GetEnergyThisTick>(entity);
        }
    }
}