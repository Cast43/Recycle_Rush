using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EffectsAuthoring : MonoBehaviour
{
    public GameObject[] effectPrefabs; // arraste aqui seus prefabs Poison, Burn, Slow…
    public class Baker : Baker<EffectsAuthoring>
    {
        public override void Bake(EffectsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            var buffer = AddBuffer<EffectPrefab>(entity);
            foreach (var go in authoring.effectPrefabs)
            {
                buffer.Add(new EffectPrefab { Prefab = GetEntity(go, TransformUsageFlags.None) });
            }
        }
    }
}
