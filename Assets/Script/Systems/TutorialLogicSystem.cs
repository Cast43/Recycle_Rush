using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
public partial struct TutorialLogicSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<MatchStateComponent>()) return;
        if (SystemAPI.GetSingleton<MatchStateComponent>().CurrentState != MatchState.Tutorial) return;

        // Garante que temos as referências dos prefabs para poder spawnar o inimigo
        if (!SystemAPI.HasSingleton<EntitiesReferences>()) return;
        var entitiesReferences = SystemAPI.GetSingleton<EntitiesReferences>();

        var enemyQuery = SystemAPI.QueryBuilder().WithAll<Enemy>().Build();
        int enemiesAlive = enemyQuery.CalculateEntityCount();

        // Criamos o ECB para poder instanciar entidades e adicionar componentes de forma segura
        var ecb = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (progress, transform, playerMove, entity) in SystemAPI.Query<RefRW<TutorialProgress>, RefRO<LocalTransform>, RefRO<MovementPlayer>>().WithEntityAccess())
        {
            if (progress.ValueRO.IsCompleted) continue;

            switch (progress.ValueRO.CurrentStep)
            {
                case 0:
                    // PASSO 0: Missão de Andar
                    if (math.lengthsq(playerMove.ValueRO.moveVector) > 0.01f)
                    {
                        progress.ValueRW.CurrentStep = 1;

                        // === SPAWN DO INIMIGO AQUI ===
                        // Instancia o prefab do inimigo (certifique-se de que "enemyPrefab" existe no seu EntitiesReferences)
                        Entity spawnedEnemy = ecb.Instantiate(entitiesReferences.enemyTutorialPrefab);

                        // Define a posição: Pega a posição do jogador e adiciona 3 unidades no eixo X e Z para não nascer em cima dele
                        float3 spawnPos = transform.ValueRO.Position + new float3(3f, 0f, 3f);
                        ecb.SetComponent(spawnedEnemy, LocalTransform.FromPosition(spawnPos));
                    }
                    break;

                case 1:
                    // PASSO 1: Missão de destruir os inimigos
                    if (enemiesAlive == 0)
                    {
                        progress.ValueRW.CurrentStep = 2;
                    }
                    break;

                case 2:
                    // PASSO 2: Missão de Coleta
                    if (SystemAPI.GetComponent<GarbageInventory>(entity).GarbageCount > 0)
                    {
                        progress.ValueRW.CurrentStep = 3;
                    }
                    break;

                case 3:
                    // PASSO 3: Missão de Reciclagem
                    // Só avança quando a sucata zerar E o seu sistema principal de XP 
                    // der o ponto de upgrade (UpgradesPending > 0).
                    if (SystemAPI.GetComponent<GarbageInventory>(entity).GarbageCount <= 0)
                    {
                        var pending = SystemAPI.GetBuffer<UpgradesPending>(entity);
                        if (pending.Length > 0)
                        {
                            progress.ValueRW.CurrentStep = 4;
                        }
                    }
                    break;

                case 4:
                    // PASSO 4: Escolher o Upgrade
                    // O jogador está com a UI aberta escolhendo. 
                    // Quando ele escolher, o seu AddUpgradeSystem vai consumir esse item do buffer.
                    // Assim que a fila zerar, o tutorial sabe que a escolha foi feita!
                    var pendingCheck = SystemAPI.GetBuffer<UpgradesPending>(entity);
                    if (pendingCheck.Length == 0)
                    {
                        progress.ValueRW.CurrentStep = 5;
                    }
                    break;

                case 5:
                    // PASSO 5: Tutorial finalizado
                    progress.ValueRW.IsCompleted = true;
                    break;
            }
        }

        // Executa todas as ações pendentes (Spawn do inimigo e adição de componentes)
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}