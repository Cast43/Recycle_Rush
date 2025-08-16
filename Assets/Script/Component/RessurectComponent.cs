using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

// [GhostComponent(PrefabType = GhostPrefabType.All)]
public struct NeedRessurection : IComponentData { }
public struct RessurectArea : IComponentData
{
    public Entity ressurectionArea;
}
public struct RessurectProperties : IComponentData
{
    public uint maxRessurectionDuration;
    public uint minTimeInArea;
    public float radius;
    public Faction team;
}
//estrutura para sincronizar a duração do tempo máximo resureição no multiplayer
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct RessurectionDuration : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
    public Entity ressurectedEntity;
}
//estrutura para sincronizar a duração da área do player no multiplayer
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct TimeInRessurectionArea : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
    public Entity areaCollider;
    public Entity ressurectedEntity;
}
//buffer para verificar quem está ressusitando o player
public struct ResetLife : IComponentData
{
    public Entity value;
}