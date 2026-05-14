using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct ServerUpgradePauseSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.HasSingleton<MatchStateComponent>()) return;

        bool shouldPause = false;

        // Ignora quem tem DestroyEntityTag e verifica apenas jogadores VIVOS lendo o CurrentHealth
        foreach (var (pendingBuffer, health) in SystemAPI.Query<DynamicBuffer<UpgradesPending>, RefRO<CurrentHealth>>().WithAll<PlayerInput>().WithNone<DestroyEntityTag>())
        {
            // O jogo pausa apenas se o jogador estiver VIVO e tiver pendências
            if (pendingBuffer.Length > 0 && health.ValueRO.value > 0)
            {
                shouldPause = true;
                break;
            }
        }

        // Atualiza a variável que será enviada para a rede
        var matchStateRW = SystemAPI.GetSingletonRW<MatchStateComponent>();
        matchStateRW.ValueRW.IsPaused = shouldPause;
    }
}