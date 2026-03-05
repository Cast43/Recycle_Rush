using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial struct RandomSpawnExperienceSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkStreamInGame>();
        state.RequireForUpdate<PlayerInput>();
        // Garante que o sistema só rode se o nosso buffer de prefabs existir
        state.RequireForUpdate<ExperiencePrefabElement>();
    }

    public void OnUpdate(ref SystemState state)
    {
        BeginSimulationEntityCommandBufferSystem.Singleton ecbSingleton = SystemAPI.GetSingleton<BeginSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        var networkTime = SystemAPI.GetSingleton<NetworkTime>();
        NetworkTick currentTick = networkTime.ServerTick;
        uint baseSeed = networkTime.ServerTick.TickIndexForValidTick;
        if (baseSeed == 0) baseSeed = 1;
        var simulationTickRate = SystemAPI.GetSingleton<ClientServerTickRate>().SimulationTickRate;

        // Pega o Singleton do Buffer em modo apenas leitura (true)
        var prefabsBuffer = SystemAPI.GetSingletonBuffer<ExperiencePrefabElement>(true);

        // Se o buffer estiver vazio, aborta para não dar erro
        if (prefabsBuffer.Length == 0) return;

        uint currentSeed = currentTick.TickIndexForValidTick;

        // Unity.Mathematics.Random não aceita seed = 0, então garantimos que seja pelo menos 1
        if (currentSeed == 0)
        {
            currentSeed = 1;
        }

        // Cria o gerador de números aleatórios com a seed segura

        foreach (var (spawnExperience, spawnCooldown, entity) in
                 SystemAPI.Query<RefRW<RandomSpawnExperience>, RefRW<RandomSpawnExperienceCooldown>>().WithEntityAccess())
        {
            if (spawnExperience.ValueRO.countExperienceSpawned >= spawnExperience.ValueRO.maxExperienceSpawned) return;

            if (!spawnCooldown.ValueRO.value.IsValid)
            {
                var initTick = currentTick;
                initTick.Add((uint)(spawnExperience.ValueRO.cooldown * simulationTickRate));
                spawnCooldown.ValueRW.value = initTick;
                continue;
            }

            if (currentTick.IsNewerThan(spawnCooldown.ValueRO.value))
            {
                var nextCooldownTick = currentTick;
                nextCooldownTick.Add((uint)(spawnExperience.ValueRO.cooldown * simulationTickRate));
                spawnCooldown.ValueRW.value = nextCooldownTick;

                // 1. Cria uma semente misturando o Tick atual e o Index da Entidade usando math.hash
                uint seed = math.hash(new uint2(currentTick.TickIndexForValidTick, (uint)entity.Index));

                // 2. Garante que a semente gerada pelo hash não seja 0
                if (seed == 0) seed = 1;

                // 3. Inicia o Random com essa semente "caótica"
                var random = new Unity.Mathematics.Random(seed);

                // Agora o sorteio vai funcionar e pegar índices diferentes!
                int randomIndex = random.NextInt(0, prefabsBuffer.Length);
                Entity prefabToSpawn = prefabsBuffer[randomIndex].Prefab;

                // Instancia o prefab sorteado
                Entity spawnedExp = ECB.Instantiate(prefabToSpawn);

                // Exemplo rápido de como colocar ele numa posição aleatória também
                float maxX = spawnExperience.ValueRO.spawnPosition.x;
                float maxZ = spawnExperience.ValueRO.spawnPosition.z;
                float3 randomPos = new float3(random.NextFloat(-maxX, maxX), 0, random.NextFloat(-maxZ, maxZ));
                ECB.SetComponent(spawnedExp, Unity.Transforms.LocalTransform.FromPosition(randomPos));
                spawnExperience.ValueRW.countExperienceSpawned++;
            }
        }
    }
}