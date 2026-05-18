using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;
using Unity.Collections;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct RandomSpawnExperienceSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<PlayerInput>();
        // Garante que o sistema só rode se o nosso buffer de prefabs existir
        state.RequireForUpdate<ExperiencePrefabElement>();
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (SystemAPI.TryGetSingleton<MatchStateComponent>(out var matchState) && matchState.IsPaused) return;

        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        var simulationTickRate = SystemAPI.GetSingleton<ClientServerTickRate>().SimulationTickRate;

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();

        // Obtém a referência ao buffer de forma compatível com Jobs
        var prefabsEntity = SystemAPI.GetSingletonEntity<ExperiencePrefabElement>();
        var prefabsLookup = SystemAPI.GetBufferLookup<ExperiencePrefabElement>(true);

        state.Dependency = new SpawnExperienceJob
        {
            currentTick = networkTime.ServerTick,
            simulationTickRate = simulationTickRate,
            prefabsEntity = prefabsEntity,
            prefabsLookup = prefabsLookup,
            ECB = ECB
        }.ScheduleParallel(state.Dependency);
    }
}

[BurstCompile]
public partial struct SpawnExperienceJob : IJobEntity
{
    [ReadOnly] public NetworkTick currentTick;
    [ReadOnly] public int simulationTickRate;
    [ReadOnly] public Entity prefabsEntity;
    [ReadOnly] public BufferLookup<ExperiencePrefabElement> prefabsLookup;
    public EntityCommandBuffer.ParallelWriter ECB;

    public void Execute(
        ref RandomSpawnExperience spawnExperience,
        ref RandomSpawnExperienceCooldown spawnCooldown,
        Entity entity,
        [ChunkIndexInQuery] int sortKey)
    {
        if (spawnExperience.countExperienceSpawned >= spawnExperience.maxExperienceSpawned) return;

        var prefabsBuffer = prefabsLookup[prefabsEntity];
        if (prefabsBuffer.Length == 0) return;

        if (!spawnCooldown.value.IsValid)
        {
            var initTick = currentTick;
            initTick.Add((uint)(spawnExperience.cooldown * simulationTickRate));
            spawnCooldown.value = initTick;
            return;
        }

        if (currentTick.IsNewerThan(spawnCooldown.value))
        {
            var nextCooldownTick = currentTick;
            nextCooldownTick.Add((uint)(spawnExperience.cooldown * simulationTickRate));
            spawnCooldown.value = nextCooldownTick;

            // 1. Cria uma semente misturando o Tick atual e o Index da Entidade usando math.hash
            uint seed = math.hash(new uint2(currentTick.TickIndexForValidTick, (uint)entity.Index));

            // 2. Garante que a semente gerada pelo hash não seja 0
            if (seed == 0) seed = 1;

            // 3. Inicia o Random com essa semente "caótica"
            var random = new Unity.Mathematics.Random(seed);

            // Agora o sorteio vai funcionar e pegar índices diferentes!
            int randomIndex = random.NextInt(0, prefabsBuffer.Length);
            Entity prefabToSpawn = prefabsBuffer[randomIndex].Prefab;

            // Instancia o prefab sorteado usando a chave de ordenação (sortKey) para multithread
            Entity spawnedExp = ECB.Instantiate(sortKey, prefabToSpawn);

            float maxX = spawnExperience.spawnPosition.x;
            float maxZ = spawnExperience.spawnPosition.z;
            float3 randomPos = new float3(random.NextFloat(-maxX, maxX), 0, random.NextFloat(-maxZ, maxZ));
            var newTransform = Unity.Transforms.LocalTransform.FromPosition(randomPos);
            
            ECB.SetComponent(sortKey, spawnedExp, newTransform);
            ECB.SetComponent(sortKey, spawnedExp, new Unity.Transforms.LocalToWorld { Value = newTransform.ToMatrix() });
            
            spawnExperience.countExperienceSpawned++;
        }
    }
}