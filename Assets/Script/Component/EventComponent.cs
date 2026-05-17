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
public struct EventPendingTag : IComponentData { }
public struct EventActiveTag : IComponentData { }
public struct EventCompletedTag : IComponentData { }
public struct EventAreaRadius : IComponentData { public float value; }

public struct EventObjective : IComponentData
{
    public EventType Type;
    public float Progress;      // 0.0 a 1.0
    public float TargetValue;   // Tempo necessário ou quantidade de inimigos
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