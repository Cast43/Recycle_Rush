using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;

public class EffectsAuthoring : MonoBehaviour
{
    // [System.Serializable]
    // public class Effects
    // {
    public GameObject[] effectsPrefab; // arraste aqui seus prefabs Poison, Burn, Slow…
                                       // public string name; // arraste aqui seus prefabs Poison, Burn, Slow…
                                       // }
                                       // public Effects[] effects;
    public class Baker : Baker<EffectsAuthoring>
    {
        public override void Bake(EffectsAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            var buffer = AddBuffer<EffectPrefab>(entity);
            foreach (var effect in authoring.effectsPrefab)
            {
                buffer.Add(new EffectPrefab
                {
                    Prefab = GetEntity(effect, TransformUsageFlags.None),
                    name = new FixedString64Bytes(effect.name)
                });
            }
        }
    }
}
