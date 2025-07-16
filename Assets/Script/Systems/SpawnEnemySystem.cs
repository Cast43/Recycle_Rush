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

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        foreach (var minionSpawnAspect in SystemAPI.Query<EnemySpawnAspect>())
        {
            minionSpawnAspect.DecrementedTimers(deltaTime);
            if (minionSpawnAspect.shouldSpawn)
            {
                SpawnRandonly(ref state);
                minionSpawnAspect.CountSpawnedInWave++;
                if (minionSpawnAspect.isWaveSpaned)
                {
                    minionSpawnAspect.ResetEnemyTimer();
                    minionSpawnAspect.ResetWaveTimer();
                    minionSpawnAspect.ResetSpawnCounter();
                }
                else
                {
                    minionSpawnAspect.ResetEnemyTimer();
                }
            }
        }
    }
    private void SpawnRandonly(ref SystemState state)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        EntitiesReferences entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();
        Entity entitiesReferencesEntity = SystemAPI.GetSingletonEntity<EntitiesReferences>();
        DynamicBuffer<EnemyPrefabElement> enemyBuffer = SystemAPI.GetBuffer<EnemyPrefabElement>(entitiesReferencesEntity);

        // Busca o prefab pelo nome só quando for atirar
        Entity projectilePrefab = Entity.Null;

        int enemyIndex = UnityEngine.Random.Range(0, enemyBuffer.Length);

        Entity enemy = enemyBuffer[enemyIndex].prefab;
        float randomX = UnityEngine.Random.Range(-10, +10);
        float randomZ = UnityEngine.Random.Range(-10, +10);
        float3 randomPosition = new float3(randomX, 1, randomZ);

        SpawnOnPosition(ECB, enemy, randomPosition);
        // Debug.Log("Spawn Enemy");
    }
    private void SpawnOnPosition(EntityCommandBuffer ECB, Entity enemy, float3 position)
    {

        Entity newEnemy = ECB.Instantiate(enemy);
        ECB.SetComponent(newEnemy, LocalTransform.FromPosition(position));
    }
}
