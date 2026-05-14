using Unity.Entities;
using Unity.NetCode;
using Unity.Physics.Systems;

// 1. Grupo Preditivo (Para Input, Movimento, Dash)
[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
public partial class PausablePredictedGroup : ComponentSystemGroup
{
    protected override void OnCreate() { base.OnCreate(); RequireForUpdate<GamePlayingTag>(); }
}

// 2. Grupo de Simulação Padrão (Ataques, Inimigos Seguindo, Spawners)
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial class PausableSimulationGroup : ComponentSystemGroup
{
    protected override void OnCreate() { base.OnCreate(); RequireForUpdate<GamePlayingTag>(); }
}

// 3. Grupo de Fim de Frame (Tiros Automáticos)
[UpdateInGroup(typeof(LateSimulationSystemGroup))]
public partial class PausableLateSimulationGroup : ComponentSystemGroup
{
    protected override void OnCreate() { base.OnCreate(); RequireForUpdate<GamePlayingTag>(); }
}

// 4. Grupo de Física (Colisões, Dano on Trigger, Slow Area)
[UpdateInGroup(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
public partial class PausablePhysicsGroup : ComponentSystemGroup
{
    protected override void OnCreate() { base.OnCreate(); RequireForUpdate<GamePlayingTag>(); }
}