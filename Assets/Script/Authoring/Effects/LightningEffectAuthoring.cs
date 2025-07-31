using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class LightningEffectAuthoring : MonoBehaviour
{
    public float duration;
    public float dmgInterval;
    public int damagePerTick;
    public float radius;
    public Faction team;
    public NetCodeConfig netCodeConfig;
    public int simulationTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;
    // public GameObject[] effectPrefabs; // arraste aqui seus prefabs Poison, Burn, Slow…

    public class Baker : Baker<LightningEffectAuthoring>
    {
        public override void Bake(LightningEffectAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new LightningEffect
            {
                duration = (uint)(authoring.duration * authoring.simulationTickRate),
                dmgInterval = (uint)(authoring.dmgInterval * authoring.simulationTickRate),
                damagePerTick = authoring.damagePerTick,
                timeSinceLastTick = 0,
                target = authoring.team,
                radius = authoring.radius,
            });

            AddBuffer<LightningDuration>(entity);
            AddBuffer<LightningDps>(entity);
        }
    }
}
