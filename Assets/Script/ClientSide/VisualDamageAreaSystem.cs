using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)] // RODA SÓ NO CLIENTE!
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct VisualDamageAreaSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Só fazemos query no Cliente lendo o que o Servidor enviou
        foreach (var (areaDamage, visualState) in SystemAPI.Query<RefRO<AreaDamage>, RefRO<AreaVisualState>>())
        {
            var startTransform = SystemAPI.GetComponentRW<LocalTransform>(areaDamage.ValueRO.start);
            var endTransform = SystemAPI.GetComponentRW<LocalTransform>(areaDamage.ValueRO.end);

            if (visualState.ValueRO.IsImpacting)
            {
                // Momento do Dano: Esconde Start, Mostra End
                startTransform.ValueRW.Scale = 0f;
                endTransform.ValueRW.Scale = 1f;
            }
            else
            {
                // Resto do tempo: Mostra Start, Esconde End
                startTransform.ValueRW.Scale = 1f;
                endTransform.ValueRW.Scale = 0f;
            }
        }
    }
}