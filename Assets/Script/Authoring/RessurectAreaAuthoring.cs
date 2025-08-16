using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class RessurectAreaAuthoring : MonoBehaviour
{
    public float maxTimeRessurect;
    public float minTimeInArea;
    public float radius;
    public Faction faction;
    public NetCodeConfig netCodeConfig;
    public int simulationTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;

    public class Baker : Baker<RessurectAreaAuthoring>
    {
        public override void Bake(RessurectAreaAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RessurectProperties
            {
                maxRessurectionDuration = (uint)(authoring.maxTimeRessurect * authoring.simulationTickRate),
                minTimeInArea = (uint)(authoring.minTimeInArea * authoring.simulationTickRate),
                radius = authoring.radius,
                team = authoring.faction,

            });
            AddComponent(entity, new Team
            {
                faction = authoring.faction,
            });
            AddBuffer<RessurectionDuration>(entity);
            // AddBuffer<TimeInRessurectionArea>(entity);
        }
    }
}