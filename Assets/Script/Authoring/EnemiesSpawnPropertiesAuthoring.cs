using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

public class EnemiesSpawnPropertiesAuthoring : MonoBehaviour
{
    public float timeBetweenWaves;
    public float timeBetweenEnemies;
    public int CountToSpawnInWave;
    public int waveStart;
    public int maxEntitiesToSpawnInWave = 10;
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
            });
            AddComponent(entity, new WaveProperties
            {
                WaveCount = authoring.waveStart,
                countEntitiesSpawned = 0,
                countMaxEntitiesToSpawn = authoring.maxEntitiesToSpawnInWave,
            });
        }
    }
}
