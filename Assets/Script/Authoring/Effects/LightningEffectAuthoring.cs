using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class LightningEffectAuthoring : MonoBehaviour
{
    public int damage;
    public int chainCount;
    public float radius;
    public Faction team;
    public GameObject particleUnit;
    // public GameObject particleArea;
    // public GameObject[] effectPrefabs; // arraste aqui seus prefabs Poison, Burn, Slow…

    public class Baker : Baker<LightningEffectAuthoring>
    {
        public override void Bake(LightningEffectAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new LightningEffect
            {
                damage = authoring.damage,
                target = authoring.team,
                radius = authoring.radius,
                chainCount = authoring.chainCount,
                sinalizationInstantiateParticle = GetEntity(authoring.particleUnit, TransformUsageFlags.None),
            });
        }
    }
}
