using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class SpawnOnDeathAuthoring : MonoBehaviour
{
    public GameObject entity;
    public float scale;

    public class Baker : Baker<SpawnOnDeathAuthoring>
    {
        public override void Bake(SpawnOnDeathAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new SpawnOnDeath
            {
                entity = GetEntity(authoring.entity, TransformUsageFlags.Dynamic),
                scale = authoring.scale,
            });
        }
    }
}