using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

public class EnemiesSpawnPropertiesAuthoring : MonoBehaviour
{
    public float timeBetweenWaves;
    public float timeBetweenEnemies;
    public int CountToSpawnInWave;
    public class Baker : Baker<EnemiesSpawnPropertiesAuthoring>
    {
        public override void Bake(EnemiesSpawnPropertiesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new EnemiesSpawnProperties
            {
                timeBetweenWaves = authoring.timeBetweenWaves,
                timeBetweenEnemies = authoring.timeBetweenEnemies,
                CountToSpawnInWave = authoring.CountToSpawnInWave,
            });
            AddComponent(entity, new EnemiesSpawnTimers
            {
                timeToNextWave = authoring.timeBetweenWaves,
                timeToNextEnemy = 0f,
                CountSpawnedInWave = 0,
            });
        }
    }
}
