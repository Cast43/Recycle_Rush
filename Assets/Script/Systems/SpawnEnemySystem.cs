using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct SpawnEnemySystem : ISystem
{
    private EntityQuery aliveEnemiesQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        aliveEnemiesQuery = SystemAPI.QueryBuilder().WithAll<Enemy>().Build();
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
            if (minionSpawnAspect.IsBossWave)
            {
                BossWave(ref state, minionSpawnAspect);
                return;
            }
            if (minionSpawnAspect.CountMaxEntitiesToSpawn == 0)
            {
                Debug.Log("coloca um valor em maxEntitiesToSpawnInWave se não gera um loop infinito");
                return;
            }
            if (minionSpawnAspect.CountEntitiesSpawned >= minionSpawnAspect.CountMaxEntitiesToSpawn && aliveEnemiesQuery.IsEmpty)
            {
                Debug.Log("terminou a wave");
                minionSpawnAspect.IncrementWaveCount();
                minionSpawnAspect.ResetEntitySpawnCounter();
                minionSpawnAspect.ResetWaveTimer();
            }
            minionSpawnAspect.DecrementedTimers(deltaTime);
            if (minionSpawnAspect.shouldSpawn && minionSpawnAspect.CountEntitiesSpawned < minionSpawnAspect.CountMaxEntitiesToSpawn)
            {
                SpawnRandonlyInRing(ref state, minionSpawnAspect.spawnCenter, minionSpawnAspect.notSpawnRadius, minionSpawnAspect.spawnRadius, minionSpawnAspect.WaveCount);
                minionSpawnAspect.IncrementEntitiesCount();
                if (minionSpawnAspect.isWaveSpaned)
                {
                    minionSpawnAspect.ResetEnemyTimer();
                }
                else
                {
                    minionSpawnAspect.ResetEnemyTimer();
                }
            }
            if (minionSpawnAspect.WaveCount % minionSpawnAspect.BossInWave == 0 && minionSpawnAspect.WaveCount != 0)
            {
                minionSpawnAspect.IsBossWave = true;
            }
        }
    }

    private void SpawnRandonlyInRing(ref SystemState state, float3 center, float innerRadius, float outerRadius, int waveCount)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

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
    private void BossWave(ref SystemState state, EnemySpawnAspect spawnAspect)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        var healthLookup = SystemAPI.GetComponentLookup<CurrentHealth>();

        // Se JÁ TEMOS um Boss instanciado, precisamos ver se ele morreu
        if (spawnAspect.SpawnedBoss != Entity.Null)
        {
            bool isDead = false;

            // 1. A CHECAGEM MAIS IMPORTANTE: A entidade do Boss ainda existe?
            // Se o sistema de dano destruiu o boss, ele não existe mais.
            if (!SystemAPI.Exists(spawnAspect.SpawnedBoss))
            {
                isDead = true;
            }
            // 2. Só checamos a vida se a entidade comprovadamente EXISTE
            else if (healthLookup.HasComponent(spawnAspect.SpawnedBoss))
            {
                if (healthLookup[spawnAspect.SpawnedBoss].value <= 0)
                {
                    isDead = true;
                }
            }
            // 3. Se a entidade existe mas perdeu o componente CurrentHealth
            else
            {
                isDead = true;
            }

            // Se o Boss morreu, resetamos as coisas para a próxima wave
            if (isDead)
            {
                spawnAspect.WaveCount++;
                spawnAspect.IsBossWave = false;

                // --- CORREÇÃO AQUI: spawnAspect.Entity ---
                var waveProperties = SystemAPI.GetComponent<WaveProperties>(spawnAspect.Self);
                waveProperties.spawnedBoss = Entity.Null;
                ECB.SetComponent(spawnAspect.Self, waveProperties);
                // ------------------------------------------

                spawnAspect.ResetEntitySpawnCounter();
                spawnAspect.ResetWaveTimer();
            }
        }
        // Se NÃO TEMOS um Boss instanciado, precisamos gerar um
        else
        {
            Entity entitiesReferencesEntity = SystemAPI.GetSingletonEntity<EntitiesReferences>();
            DynamicBuffer<BossPrefabElement> bossBuffer = SystemAPI.GetBuffer<BossPrefabElement>(entitiesReferencesEntity);

            int enemyIndex = UnityEngine.Random.Range(0, bossBuffer.Length);
            Entity bossPrefab = bossBuffer[enemyIndex].prefab;

            // Instancia o novo boss
            Entity instantiatedBoss = SpawnBoss(ECB, bossPrefab, spawnAspect.bossSpawnPosition);

            // --- CORREÇÃO AQUI: spawnAspect.Entity ---
            var waveProperties = SystemAPI.GetComponent<WaveProperties>(spawnAspect.Self);
            waveProperties.spawnedBoss = instantiatedBoss;
            ECB.SetComponent(spawnAspect.Self, waveProperties);
            // ------------------------------------------
        }
    }

    // Mudamos o retorno de void para Entity
    private Entity SpawnBoss(EntityCommandBuffer ECB, Entity bossPrefab, float3 position)
    {
        Entity bossEntity = ECB.Instantiate(bossPrefab);
        ECB.SetComponent(bossEntity, LocalTransform.FromPosition(position));
        return bossEntity; // Retornamos a entidade real que está no jogo
    }
}
