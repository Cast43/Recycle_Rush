using Unity.Entities;
using UnityEngine;
using Unity.NetCode;
using Unity.Physics;

// Roda em todas as simulações simultaneamente (Server e Client)
[UpdateInGroup(typeof(InitializationSystemGroup))]
public partial class ApplyPauseStateSystem : SystemBase
{
    private EntityQuery tagQuery;

    protected override void OnCreate()
    {
        tagQuery = EntityManager.CreateEntityQuery(typeof(GamePlayingTag));
    }

    protected override void OnUpdate()
    {
        if (!SystemAPI.HasSingleton<MatchStateComponent>()) return;

        // Lê a variável sincronizada pelo servidor
        bool isPaused = SystemAPI.GetSingleton<MatchStateComponent>().IsPaused;

        // 1. Congela a Física do DOTS (Tiros, pulos e colisões param no ar perfeitamente)
        if (SystemAPI.HasSingleton<PhysicsStep>())
        {
            var physicsStep = SystemAPI.GetSingletonRW<PhysicsStep>();
            physicsStep.ValueRW.SimulationType = isPaused ? SimulationType.NoPhysics : SimulationType.UnityPhysics;
        }

        // 2. Cria ou destrói a tag de controle de pausa para os seus sistemas lógicos
        bool hasTag = !tagQuery.IsEmptyIgnoreFilter;

        if (isPaused && hasTag)
        {
            EntityManager.DestroyEntity(tagQuery);
        }
        else if (!isPaused && !hasTag)
        {
            EntityManager.CreateEntity(typeof(GamePlayingTag));
        }
    }
}