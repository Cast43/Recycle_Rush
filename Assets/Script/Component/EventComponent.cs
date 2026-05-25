using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Mathematics;


[InternalBufferCapacity(8)]
public struct EventsPrefabElement : IBufferElementData
{
    public Entity Prefab;
}
// Tipos de recompensas/eventos
public enum EventType { Cleanup, KingOfTheHill, Transport }

// Tags de Estado do Evento
[GhostComponent]
public struct EventPendingTag : IComponentData { }
[GhostComponent]
public struct EventActiveTag : IComponentData { }
[GhostComponent]
public struct EventCompletedTag : IComponentData { }
[GhostComponent]
public struct EventAreaRadius : IComponentData { [GhostField] public float value; }

[GhostComponent]
public struct EventObjective : IComponentData
{
    [GhostField] public EventType Type;
    [GhostField] public float Progress;      // 0.0 a 1.0
    [GhostField] public float TargetValue;   // Tempo necessário ou quantidade de inimigos
    [GhostField] public float TimeLimit;     // Tempo máximo configurado no Inspector
    [GhostField] public float TimeRemaining; // Tempo restante sincronizado para a HUD
}

// Componentes Específicos de cada Evento
public struct KingOfTheHillData : IComponentData
{
    public float Progress;
    public float TargetTime;
}
public struct CleanupData : IComponentData
{
    public int EnemiesRemaining;
}
public struct TransportData : IComponentData
{
    public Entity ObjectToCarry;
    public float3 TargetPosition;
}
public struct EventSpawnerState : IComponentData
{
    public int CurrentWave;         // A wave em que o jogo está agora
    public int LastWaveSpawned;     // A última wave em que um evento foi criado
}