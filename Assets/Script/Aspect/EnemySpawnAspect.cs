using Unity.Entities;

public readonly partial struct EnemySpawnAspect : IAspect
{
    private readonly RefRW<EnemiesSpawnTimers> _enemySpawnTimers;
    private readonly RefRW<WaveCount> waveCount;
    private readonly RefRO<EnemiesSpawnProperties> _enemySpawnProperties;

    public int CountSpawnedInWave
    {
        get => _enemySpawnTimers.ValueRO.CountSpawnedInWave;
        set => _enemySpawnTimers.ValueRW.CountSpawnedInWave = value;
    }
    public int WaveCount
    {
        get => waveCount.ValueRO.value;
        set => waveCount.ValueRW.value = value;
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

    private int CountToSpawnInWave => _enemySpawnProperties.ValueRO.CountToSpawnInWave;
    private float timeBetweenEnemies => _enemySpawnProperties.ValueRO.timeBetweenEnemies;
    private float timeBetweenWaves => _enemySpawnProperties.ValueRO.timeBetweenWaves;

    public bool shouldSpawn => timeToNextWave <= 0f && TimeToNextEnemy <= 0f;
    public bool isWaveSpaned => CountSpawnedInWave >= CountToSpawnInWave;

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

    public void ResetSpawnCounter()
    {
        CountSpawnedInWave = 0;
    }

    public void IncrementWaveCount()
    {
        waveCount.ValueRW.value++;
    }
}
