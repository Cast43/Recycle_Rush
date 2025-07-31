using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class PoisonEffectAuthoring : MonoBehaviour
{
    public float duration;
    public float dmgInterval;
    public int damagePerTick;
    public NetCodeConfig netCodeConfig;
    public int simulationTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;

    public class Baker : Baker<PoisonEffectAuthoring>
    {
        public override void Bake(PoisonEffectAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PoisonEffect
            {
                duration = (uint)(authoring.duration * authoring.simulationTickRate),
                dmgInterval = (uint)(authoring.dmgInterval * authoring.simulationTickRate),
                damagePerTick = authoring.damagePerTick,
                timeSinceLastTick = 0
            });
            AddBuffer<PoisonDuration>(entity);
            AddBuffer<PoisonDps>(entity);
        }
    }
}
