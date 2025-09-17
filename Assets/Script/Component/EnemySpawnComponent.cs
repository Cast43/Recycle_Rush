using Unity.Entities;
using Unity.Mathematics;

public struct EnemiesSpawnProperties : IComponentData
{
    public float timeBetweenWaves;
    public float timeBetweenEnemies;
    public int CountToSpawnInWave;
    public float3 spawnCenter;
    public float notSpawnRadius;
    public float SpawnRadius;

}

public struct EnemiesSpawnTimers : IComponentData
{
    public float timeToNextWave;
    public float timeToNextEnemy;
}

public struct WaveProperties : IComponentData
{
    public int WaveCount;
    public int countEntitiesSpawned;
    public int countMaxEntitiesToSpawn;
}
