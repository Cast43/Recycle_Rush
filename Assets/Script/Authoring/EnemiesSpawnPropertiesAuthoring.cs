using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EnemiesSpawnPropertiesAuthoring : MonoBehaviour
{
    public float timeBetweenWaves;
    public float timeBetweenEnemies;
    public int CountToSpawnInWave;
    public int waveStart;
    public int maxEntitiesToSpawnInWave = 10;
    public float3 spawnCenter = float3.zero;
    public int notSpawnRadius = 20;
    public int SpawnRadius = 30;
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
                spawnCenter = authoring.spawnCenter,
                notSpawnRadius = authoring.notSpawnRadius,
                SpawnRadius = authoring.SpawnRadius
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
