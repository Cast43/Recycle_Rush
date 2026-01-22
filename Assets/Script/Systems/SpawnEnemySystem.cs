using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct SpawnEnemySystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {

    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        SpawnOverTime(ref state);
    }

    private void SpawnOverTime(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var minionSpawnAspect in SystemAPI.Query<EnemySpawnAspect>())
        {
            if (minionSpawnAspect.CountMaxEntitiesToSpawn == 0)
            {
                Debug.Log("coloca um valor em maxEntitiesToSpawnInWave se não gera um loop infinito");
                return;
            }
            if (minionSpawnAspect.CountEntitiesSpawned >= minionSpawnAspect.CountMaxEntitiesToSpawn)
            {
                Debug.Log("terminou a wave");
                minionSpawnAspect.IncrementWaveCount();
                minionSpawnAspect.ResetEntitySpawnCounter();
            }
            minionSpawnAspect.DecrementedTimers(deltaTime);
            if (minionSpawnAspect.shouldSpawn)
            {
                SpawnRandonlyInRing(ref state, minionSpawnAspect.spawnCenter, minionSpawnAspect.notSpawnRadius, minionSpawnAspect.spawnRadius, minionSpawnAspect.WaveCount);
                minionSpawnAspect.IncrementEntitiesCount();
                if (minionSpawnAspect.isWaveSpaned)
                {
                    minionSpawnAspect.ResetEnemyTimer();
                    minionSpawnAspect.ResetWaveTimer();
                }
                else
                {
                    minionSpawnAspect.ResetEnemyTimer();
                }
            }
        }
    }

    private void SpawnRandonlyInRing(ref SystemState state, float3 center, float innerRadius, float outerRadius, int waveCount)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        Entity entitiesReferencesEntity = SystemAPI.GetSingletonEntity<EntitiesReferences>();
        DynamicBuffer<EnemyPrefabElement> enemyBuffer = SystemAPI.GetBuffer<EnemyPrefabElement>(entitiesReferencesEntity);

        int enemyIndex = UnityEngine.Random.Range(0, enemyBuffer.Length);
        Entity enemy = enemyBuffer[enemyIndex].prefab;

        float angle = UnityEngine.Random.Range(0f, math.PI * 2f);
        float r = math.sqrt(UnityEngine.Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
        float3 offset = new float3(r * math.cos(angle), 0f, r * math.sin(angle));
        float3 randomPosition = center + offset;

        // var enemyXpLookup = SystemAPI.GetComponentLookup<CurrentExperience>();
        // var enemyCurrentXp = enemyXpLookup[enemy];
        // for (int i = 0; i < waveCount; i++)
        {
            ECB.SetComponent(enemy, new Level { current = waveCount });
        }


        SpawnOnPosition(ECB, enemy, randomPosition);
        // Debug.Log("Spawn Enemy");
    }
    private void SpawnOnPosition(EntityCommandBuffer ECB, Entity enemy, float3 position)
    {

        Entity newEnemy = ECB.Instantiate(enemy);
        ECB.SetComponent(newEnemy, LocalTransform.FromPosition(position));
    }
}
