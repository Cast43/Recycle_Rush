using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class CurseEffectAuthoring : MonoBehaviour
{
    public float duration;
    public int cursePerStack;
    public int maxStacks;
    public NetCodeConfig netCodeConfig;
    public int simulationTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;

    public class Baker : Baker<CurseEffectAuthoring>
    {
        public override void Bake(CurseEffectAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new CurseEffect
            {
                duration = (uint)(authoring.duration * authoring.simulationTickRate),
                addAmmount = (uint)authoring.cursePerStack,
                maxStacks = (uint)authoring.maxStacks,
            });
        }
    }
}
