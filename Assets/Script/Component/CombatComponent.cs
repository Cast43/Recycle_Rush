using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Collections;

public struct Team : IComponentData
{
    [GhostField] public Faction faction;
}
public struct MaxHealth : IComponentData
{
    public int value;
}

public struct CurrentHealth : IComponentData
{
    [GhostField] public int value;
    [GhostField] public bool onHealthChanged;
}

public struct HealthBar : IComponentData
{
    public Entity barVisualEntity;
    public Entity healthEntity;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct DamageBufferElement : IBufferElementData
{
    public int value;
    public Entity owner;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct DamageThisTick : ICommandData
{
    public NetworkTick Tick { get; set; }
    public int value;
    public Entity owner;
}

public struct DestroyOnTimer : IComponentData
{
    [GhostField] public float value;
}
public struct DestroyAtTick : IComponentData
{
    [GhostField] public NetworkTick value;
}
public struct DestroyEntityTag : IComponentData { }

public struct DamageOnTrigger : IComponentData
{
    public int value;
}
public struct AlreadyDamagedEntity : IBufferElementData
{
    public Entity value;
}
public struct ShootAttackProperties : IComponentData
{
    public float3 firePointOffset;
    public uint cooldownTickCount;
    public uint timeToDontMove;
    // public FixedString64Bytes throwableName;
    public Entity attackPrefab;
}

public struct Owner : IComponentData
{
    public Entity Value;
}

public struct MeleeAttackProperties : IComponentData
{
    public uint cooldownTickCount;
    public int damage;
}


public struct TargetRadius : IComponentData
{
    public float value;
}
public struct TargetFind : IComponentData
{
    [GhostField] public Faction value;
}
public struct TargetEntity : IComponentData
{
    [GhostField] public Entity value;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct ShootAttackCooldown : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
}

public struct ShootCooldownTick : IComponentData
{
    public uint shoot;
}

public struct MeleeAttackCooldown : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
}

public struct MeleeCooldownTick : IComponentData
{
    public uint melee;
}
