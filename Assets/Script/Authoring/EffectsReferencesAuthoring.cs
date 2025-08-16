using Unity.Entities;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;



public class EffectsReferencesAuthoring : MonoBehaviour
{
    public GameObject[] effectPrefabs; // arraste aqui seus prefabs Poison, Burn, Slow…
    // public Entity playerOwner; // guarda a referência do player para analizar qual upgrade ele já possui
    public class Baker : Baker<EffectsReferencesAuthoring>
    {
        public override void Bake(EffectsReferencesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            var buffer = AddBuffer<GlobalEffectPrefab>(entity);
            foreach (var go in authoring.effectPrefabs)
            {
                buffer.Add(new GlobalEffectPrefab
                {
                    Prefab = GetEntity(go, TransformUsageFlags.None),
                    name = new FixedString64Bytes(go.name)
                });
            }
        }
    }
}