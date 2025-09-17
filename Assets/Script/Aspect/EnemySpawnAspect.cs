using Unity.Entities;
using Unity.Mathematics;

public readonly partial struct EnemySpawnAspect : IAspect
{
    private readonly RefRW<EnemiesSpawnTimers> _enemySpawnTimers;
    private readonly RefRW<WaveProperties> waveProperties;
    private readonly RefRO<EnemiesSpawnProperties> _enemySpawnProperties;

    public int CountEntitiesSpawned
    {
        get => waveProperties.ValueRO.countEntitiesSpawned;
        set => waveProperties.ValueRW.countEntitiesSpawned = value;
    }
    public int WaveCount
    {
        get => waveProperties.ValueRO.WaveCount;
        set => waveProperties.ValueRW.WaveCount = value;
    }

    private float TimeToNextEnemy
    {
        get => _enemySpawnTimers.ValueRO.timeToNextEnemy;
        set => _enemySpawnTimers.ValueRW.timeToNextEnemy = value;
    }

    private float timeToNextWave
    {
        get => _enemySpawnTimers.ValueRO.timeToNextWave;
        set => _enemySpawnTimers.ValueRW.timeToNextWave = value;
    }

    public int CountMaxEntitiesToSpawn => waveProperties.ValueRO.countMaxEntitiesToSpawn;
    private int CountToSpawnInWave => _enemySpawnProperties.ValueRO.CountToSpawnInWave;
    private float timeBetweenEnemies => _enemySpawnProperties.ValueRO.timeBetweenEnemies;
    private float timeBetweenWaves => _enemySpawnProperties.ValueRO.timeBetweenWaves;
    public float3 spawnCenter => _enemySpawnProperties.ValueRO.spawnCenter;
    public float notSpawnRadius => _enemySpawnProperties.ValueRO.notSpawnRadius;
    public float spawnRadius => _enemySpawnProperties.ValueRO.SpawnRadius;

    public bool shouldSpawn => timeToNextWave <= 0f && TimeToNextEnemy <= 0f;
    public bool isWaveSpaned => CountEntitiesSpawned >= CountToSpawnInWave;

    public void DecrementedTimers(float deltaTime)
    {
        if (timeToNextWave >= 0f)
        {
            timeToNextWave -= deltaTime;
            return;
        }
        if (TimeToNextEnemy >= 0f)
        {
            TimeToNextEnemy -= deltaTime;
        }
    }

    public void ResetWaveTimer()
    {
        timeToNextWave = timeBetweenWaves;
    }

    public void ResetEnemyTimer()
    {
        TimeToNextEnemy = timeBetweenEnemies;
    }

    public void ResetEntitySpawnCounter()
    {
        CountEntitiesSpawned = 0;
    }

    public void IncrementWaveCount()
    {
        waveProperties.ValueRW.WaveCount++;
    }
    public void IncrementEntitiesCount()
    {
        waveProperties.ValueRW.countEntitiesSpawned++;
    }

}
