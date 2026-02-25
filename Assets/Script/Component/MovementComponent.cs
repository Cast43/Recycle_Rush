using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct MovementPlayer : IInputComponentData
{
    public float3 moveVector;
}
public struct Movement : IComponentData
{
    public float3 moveVector;
}
public struct MoveSpeed : IComponentData
{
    [GhostField] public float maxSpeed;
    [GhostField] public float currentSpeed;
}
public struct DashProperties : IComponentData
{
    [GhostField] public int lostEnergy;
    [GhostField] public float cooldown;
    [GhostField] public float duration;
    [GhostField] public float speed;
    [GhostField] public bool canDash;
    [GhostField] public bool isDashing;
}
public struct EnemyDashProperties : IComponentData
{
    public float aggroDistance;
    public float cooldown;
    public float duration;
    public float speed;
    public bool isDashing;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct DashCooldown : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct DashDuration : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct DashCommand : ICommandData
{
    public NetworkTick Tick { get; set; }
    public float3 DashDirection;
}

public struct Direction : IComponentData
{
    [GhostField] public float3 lookDirection;
}
public struct Rotation : IComponentData
{
    public float rotationSpeed;
    public float Yoffset;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct DontMoveOnTimer : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
}