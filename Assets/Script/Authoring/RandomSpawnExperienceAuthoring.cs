using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class RandomSpawnMateriaisAuthoring : MonoBehaviour
{
    public float cooldown;
    public int maxExperienceSpawned;
    public int countExperienceSpawned;
    public float3 spawnPosition;
    public class Baker : Baker<RandomSpawnMateriaisAuthoring>
    {
        public override void Bake(RandomSpawnMateriaisAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RandomSpawnExperience
            {
                cooldown = authoring.cooldown,
                maxExperienceSpawned = authoring.maxExperienceSpawned,
                countExperienceSpawned = authoring.countExperienceSpawned,
                spawnPosition = authoring.spawnPosition,
            });
            AddComponent(entity, new RandomSpawnExperienceCooldown { });
        }
    }
}