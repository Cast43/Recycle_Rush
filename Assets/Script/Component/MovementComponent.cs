using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

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
    public float value;
}
public struct DashProperties : IComponentData
{
    public uint cooldown;
    public uint duration;
    public float speed;
    [GhostField] public bool canDash;
    [GhostField] public bool isDashing;
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
public struct DashVector : IComponentData
{
    public float3 dashVector;
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