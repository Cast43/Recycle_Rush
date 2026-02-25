using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Transforms;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct ServerRestartHandlerSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        // --- PARTE A: Escutar o RPC ---
        foreach (var (rpc, entity) in SystemAPI.Query<RestartGameRpc>().WithEntityAccess())
        {
            UnityEngine.Debug.Log("Servidor: Reiniciando...");

            // 1. Destruir TODOS os jogadores antigos antes de criar novos
            // Fazemos isso fora do loop de NetworkId para não tentar destruir o mesmo boneco 2 vezes
            foreach (var (input, playerEntity) in SystemAPI.Query<PlayerInput>().WithEntityAccess())
            {
                ecb.DestroyEntity(playerEntity);
            }
            foreach (var (enemy, enemyEntity) in SystemAPI.Query<Enemy>().WithEntityAccess())
            {
                ecb.DestroyEntity(enemyEntity);
            }
            foreach (var (ressurect, ressurectEntity) in SystemAPI.Query<RessurectProperties>().WithEntityAccess())
            {
                ecb.DestroyEntity(ressurectEntity);
            }
            foreach (var (shoot, shootEntity) in SystemAPI.Query<Arrow>().WithEntityAccess())
            {
                ecb.DestroyEntity(shootEntity);
            }
            foreach (var (experience, experienceEntity) in SystemAPI.Query<GiveExperience>().WithEntityAccess())
            {
                ecb.DestroyEntity(experienceEntity);
            }

            // 2. Pegar a Configuração (Fora do loop, pois é única)
            if (SystemAPI.TryGetSingleton<GameConfig>(out var config))
            {
                // 3. Loop pelas CONEXÕES para criar novos bonecos
                foreach (var (netId, connEntity) in SystemAPI.Query<RefRO<NetworkId>>().WithEntityAccess())
                {
                    // A. Instancia o Prefab
                    var newPlayer = ecb.Instantiate(config.playerPrefabA);

                    // B. --- A CORREÇÃO DO ERRO ESTÁ AQUI ---
                    // Definimos quem é o dono desse Ghost imediatamente.
                    ecb.SetComponent(newPlayer, new GhostOwner { NetworkId = netId.ValueRO.Value });

                    // C. (Opcional) Definir posição inicial e linkar para destruir se desconectar
                    ecb.SetComponent(newPlayer, LocalTransform.FromPosition(0, 1, 0));
                    ecb.AppendToBuffer(connEntity, new LinkedEntityGroup { Value = newPlayer });
                }
            }

            // 4. Resetar a Wave
            if (SystemAPI.TryGetSingletonRW<WaveProperties>(out var waveProp))
            {
                waveProp.ValueRW.WaveCount = 0;
            }

            // 5. Destrói o RPC
            ecb.DestroyEntity(entity);
        }

        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}