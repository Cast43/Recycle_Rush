using Unity.Entities;
using Unity.Mathematics;

public readonly partial struct EnemySpawnAspect : IAspect
{
    private readonly RefRW<EnemiesSpawnTimers> _enemySpawnTimers;
    private readonly RefRW<WaveProperties> waveProperties;
    private readonly RefRW<EnemiesSpawnProperties> _enemySpawnProperties;
    public readonly Entity Self;
    public int BossInWave
    {
        get => waveProperties.ValueRO.bossInWave;
        set => waveProperties.ValueRW.bossInWave = value;
    }

    public bool IsBossWave
    {
        get => waveProperties.ValueRO.isBossWave;
        set => waveProperties.ValueRW.isBossWave = value;
    }
    public Entity SpawnedBoss
    {
        get => waveProperties.ValueRO.spawnedBoss;
        set => waveProperties.ValueRW.spawnedBoss = value;
    }
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

    private float delaySpawnNextWave
    {
        get => _enemySpawnTimers.ValueRO.delaySpawnNextWave;
        set => _enemySpawnTimers.ValueRW.delaySpawnNextWave = value;
    }

    public int CountMaxEntitiesToSpawn
    {
        get => waveProperties.ValueRO.countMaxEntitiesToSpawn;
        set => waveProperties.ValueRW.countMaxEntitiesToSpawn = value;
    }

    public float timeBetweenEnemiesModifier => waveProperties.ValueRO.modifierTimeBetweenEnemies;
    public float modifierMaxEnemiesSpawn => waveProperties.ValueRO.modifierMaxEnemiesSpawn;
    private int CountToSpawnInWave => _enemySpawnProperties.ValueRO.CountToSpawnInWave;
    private float timeBetweenEnemies
    {
        get => _enemySpawnProperties.ValueRO.timeBetweenEnemies;
        set => _enemySpawnProperties.ValueRW.timeBetweenEnemies = value;
    }
    private float countDelaySpawnNextWave => _enemySpawnProperties.ValueRO.delaySpawnNextWave;
    public float3 spawnCenter => _enemySpawnProperties.ValueRO.spawnCenter;
    public float notSpawnRadius => _enemySpawnProperties.ValueRO.notSpawnRadius;
    public float spawnRadius => _enemySpawnProperties.ValueRO.SpawnRadius;
    public float bossSpawnPosition => _enemySpawnProperties.ValueRO.BossSpawnPosition;

    public bool shouldSpawn => delaySpawnNextWave <= 0f && TimeToNextEnemy <= 0f;
    public bool isWaveSpaned => CountEntitiesSpawned >= CountToSpawnInWave;
    public bool bossWave => CountEntitiesSpawned >= CountToSpawnInWave;

    public void DecrementedTimers(float deltaTime)
    {
        if (delaySpawnNextWave >= 0f)
        {
            delaySpawnNextWave -= deltaTime;
            return;
        }
        if (TimeToNextEnemy >= 0f)
        {
            TimeToNextEnemy -= deltaTime;
        }
    }

    public void ResetWaveTimer()
    {
        delaySpawnNextWave = countDelaySpawnNextWave;
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
        // waveProperties.ValueRW.countMaxEntitiesToSpawn = (int)(waveProperties.ValueRW.countMaxEntitiesToSpawn * modifier);
        timeBetweenEnemies *= timeBetweenEnemiesModifier;
        CountMaxEntitiesToSpawn = (int)(CountMaxEntitiesToSpawn * modifierMaxEnemiesSpawn);
    }
    public void IncrementEntitiesCount()
    {
        waveProperties.ValueRW.countEntitiesSpawned++;
    }

}
