using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PausableSimulationGroup))]
public partial struct SpawnEnemySystem : ISystem
{
    private EntityQuery aliveEnemiesQuery;
    private EntityQuery activeEventsQuery;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        aliveEnemiesQuery = SystemAPI.QueryBuilder().WithAll<Enemy>().Build();
        activeEventsQuery = SystemAPI.QueryBuilder().WithAll<EventActiveTag>().Build();

        // Garante que o sistema só rode se existir um gerenciador de partida
        state.RequireForUpdate<MatchStateComponent>();
    }
    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var ms) && ms.IsPaused) return;

        // Pega o estado atual da partida
        var matchState = SystemAPI.GetSingleton<MatchStateComponent>().CurrentState;

        // SÓ PERMITE O SPAWN SE A PARTIDA ESTIVER VALENDO (Tutorial já acabou)
        if (matchState != MatchState.Playing) return;

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
                minionSpawnAspect.ResetExperienceSpawned();
            }
            minionSpawnAspect.DecrementedTimers(deltaTime);
            if (minionSpawnAspect.shouldSpawn && minionSpawnAspect.CountEntitiesSpawned < minionSpawnAspect.CountMaxEntitiesToSpawn)
            {
                SpawnRandonlyInRing(ref state, minionSpawnAspect.notSpawnRadius, minionSpawnAspect.spawnRadius, minionSpawnAspect.WaveCount);
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

            // 1. Pega os dados de controle com permissão de Leitura e Escrita (RW = Read/Write)
            if (!SystemAPI.TryGetSingletonRW<EventSpawnerState>(out var spawnerState)) break;
            spawnerState.ValueRW.CurrentWave = minionSpawnAspect.WaveCount;

            // 3. A NOVA MÁGICA: Só spawna se for a wave certa E ainda não tiver spawnado nela
            if (minionSpawnAspect.WaveCount % minionSpawnAspect.EventInWave == 0 && spawnerState.ValueRO.LastWaveSpawned < spawnerState.ValueRO.CurrentWave)
            {
                if (minionSpawnAspect.WaveCount != 0)
                {
                    // Verifica se a query está vazia, ou seja, se NÃO EXISTE nenhum evento rodando
                    if (activeEventsQuery.IsEmpty)
                    {
                        float3 centro = float3.zero; // Posição base

                        SpawnEvent(ref state, centro, 15f); 

                        // 4. ATUALIZA O ESTADO: Salva que já geramos o evento desta wave!
                        // Assim, mesmo que o evento seja destruído, este 'if' não será verdadeiro de novo.
                        spawnerState.ValueRW.LastWaveSpawned = minionSpawnAspect.WaveCount;
                    }
                }
            }
        }
    }
    private void SpawnEvent(ref SystemState state, float3 centerPosition, float spawnRadius)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        Entity entitiesReferencesEntity = SystemAPI.GetSingletonEntity<EntitiesReferences>();
        DynamicBuffer<EventsPrefabElement> eventsBuffer = SystemAPI.GetBuffer<EventsPrefabElement>(entitiesReferencesEntity);

        // 1. Inicializa o Random do DOTS
        // Usamos o tempo decorrido como semente (seed) para garantir que sempre seja aleatório.
        // (A semente não pode ser 0, por isso o +1).
        uint seed = (uint)(SystemAPI.Time.ElapsedTime * 1000) + 1;
        Unity.Mathematics.Random random = new Unity.Mathematics.Random(seed);

        // 2. Sorteia o evento
        int eventIndex = random.NextInt(0, eventsBuffer.Length);
        Entity eventPrefab = eventsBuffer[eventIndex].Prefab;

        // 3. Calcula a posição aleatória em um círculo (Plano XZ)
        float2 randomDir = random.NextFloat2Direction(); // Pega uma direção 2D aleatória
        float randomDistance = random.NextFloat(0f, spawnRadius); // Pega uma distância aleatória até o limite

        // Aplica a direção e a distância no eixo X e Z, mantendo o Y original
        float3 randomPosition = centerPosition + new float3(randomDir.x * randomDistance, 0f, randomDir.y * randomDistance);

        // 4. Instancia e posiciona
        Entity newEvent = ECB.Instantiate(eventPrefab);
        ECB.SetComponent(newEvent, LocalTransform.FromPosition(randomPosition));

        // O evento já nasce com EventActiveTag através do Baker.
        // Não precisamos alterar tags aqui, o que mantém o Netcode perfeitamente sincronizado.
    }

    private void SpawnRandonlyInRing(ref SystemState state, float innerRadius, float outerRadius, int waveCount)
    {
        var ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        // 1. Buscar todos os Players (supondo que tenham um componente LocalTransform e um PlayerTag)
        // Se você não tiver um "PlayerTag", use o componente que identifica seu player.
        EntityQuery playerQuery = SystemAPI.QueryBuilder().WithAll<LocalTransform, PlayerInput>().Build();
        var players = playerQuery.ToEntityArray(Allocator.Temp);

        if (players.Length == 0) return; // Nenhum player no mapa, cancela spawn

        // 2. Escolher um player aleatório
        int randomPlayerIndex = UnityEngine.Random.Range(0, players.Length);
        Entity targetPlayer = players[randomPlayerIndex];
        float3 playerPosition = state.EntityManager.GetComponentData<LocalTransform>(targetPlayer).Position;

        // 3. Pegar o Prefab do Buffer
        Entity entitiesReferencesEntity = SystemAPI.GetSingletonEntity<EntitiesReferences>();
        DynamicBuffer<EnemyPrefabElement> enemyBuffer = SystemAPI.GetBuffer<EnemyPrefabElement>(entitiesReferencesEntity);

        int enemyIndex = UnityEngine.Random.Range(0, enemyBuffer.Length);
        Entity enemyPrefab = enemyBuffer[enemyIndex].prefab;

        // 4. Lógica do Anel (Mantida como a original, mas usando playerPosition como centro)
        float angle = UnityEngine.Random.Range(0f, math.PI * 2f);
        float r = math.sqrt(UnityEngine.Random.Range(innerRadius * innerRadius, outerRadius * outerRadius));
        float3 offset = new float3(r * math.cos(angle), 0f, r * math.sin(angle));
        float3 randomPosition = playerPosition + offset;

        // 5. Configurar e Spawnar
        // Nota: Use Instantiate para não modificar o prefab original
        Entity newEnemy = ECB.Instantiate(enemyPrefab);
        ECB.SetComponent(newEnemy, new Level { current = waveCount });
        ECB.SetComponent(newEnemy, LocalTransform.FromPosition(randomPosition));

        players.Dispose(); // Importante limpar o array temporário
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
