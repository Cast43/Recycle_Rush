using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EnemiesSpawnPropertiesAuthoring : MonoBehaviour
{
    public float delaySpawnNextWave;
    public float timeBetweenEnemies;
    public float modifierTimeBetweenEnemies;
    public float modifierMaxEnemiesSpawn;
    public int CountToSpawnInWave;
    public int waveStart;
    public int maxEntitiesToSpawnInWave = 10;
    public float3 spawnCenter = float3.zero;
    public int notSpawnRadius = 20;
    public int SpawnRadius = 30;
    public int countBoss = 5;
    public int eventInWave = 2;
    public class Baker : Baker<EnemiesSpawnPropertiesAuthoring>
    {
        public override void Bake(EnemiesSpawnPropertiesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.None);
            AddComponent(entity, new EnemiesSpawnProperties
            {
                delaySpawnNextWave = authoring.delaySpawnNextWave,
                timeBetweenEnemies = authoring.timeBetweenEnemies,
                CountToSpawnInWave = authoring.CountToSpawnInWave,
                spawnCenter = authoring.spawnCenter,
                notSpawnRadius = authoring.notSpawnRadius,
                SpawnRadius = authoring.SpawnRadius
            });
            AddComponent(entity, new EnemiesSpawnTimers
            {
                delaySpawnNextWave = authoring.timeBetweenEnemies,
                timeToNextEnemy = authoring.timeBetweenEnemies,
            });
            AddComponent(entity, new WaveProperties
            {
                WaveCount = authoring.waveStart,
                countEntitiesSpawned = 0,
                countMaxEntitiesToSpawn = authoring.maxEntitiesToSpawnInWave,
                modifierTimeBetweenEnemies = authoring.modifierTimeBetweenEnemies,
                modifierMaxEnemiesSpawn = authoring.modifierMaxEnemiesSpawn,
                bossInWave = authoring.countBoss,
                eventInWave = authoring.eventInWave,
            });
            AddComponent(entity, new EventSpawnerState
            {
                CurrentWave = 0,
                LastWaveSpawned = -1,
            });
        }
    }
}
