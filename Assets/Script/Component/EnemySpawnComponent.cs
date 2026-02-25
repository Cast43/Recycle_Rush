using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;

public struct EnemiesSpawnProperties : IComponentData
{
    public float delaySpawnNextWave;
    public float timeBetweenEnemies;
    public int CountToSpawnInWave;
    public float3 spawnCenter;
    public float notSpawnRadius;
    public float SpawnRadius;
    public float BossSpawnPosition;

}

public struct EnemiesSpawnTimers : IComponentData
{
    public float delaySpawnNextWave;
    public float timeToNextEnemy;
}

public struct WaveProperties : IComponentData
{
    [GhostField] public int WaveCount;
    public int countEntitiesSpawned;
    public int countMaxEntitiesToSpawn;
    public int bossInWave;
    public float modifierTimeBetweenEnemies;
    public float modifierMaxEnemiesSpawn;
    public bool isBossWave;
    public Entity spawnedBoss;
}

[InternalBufferCapacity(4)]
public struct EnemyPrefabElement : IBufferElementData
{
    public FixedString64Bytes name;
    public Entity prefab;
}

[InternalBufferCapacity(4)]
public struct BossPrefabElement : IBufferElementData
{
    public FixedString64Bytes name;
    public Entity prefab;
}
