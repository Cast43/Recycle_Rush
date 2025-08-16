using Unity.Entities;

public struct EnemiesSpawnProperties : IComponentData
{
    public float timeBetweenWaves;
    public float timeBetweenEnemies;
    public int CountToSpawnInWave;

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
