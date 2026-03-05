using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;
using Unity.Rendering;
using Unity.Collections;

public struct Team : IComponentData
{
    [GhostField] public Faction faction;
}
public struct MaxHealth : IComponentData
{
    [GhostField] public int value;
}

public struct CurrentHealth : IComponentData
{
    [GhostField] public int value;
    [GhostField] public bool onHealthChanged;
}

public struct HealthRegen : IComponentData
{
    public int amount;
    public float cooldownRestore;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct HealthRegenCooldown : IComponentData
{
    [GhostField]
    public NetworkTick value;
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
    public uint lostEnergy;
    public float bulletLifeTime;
    public float bulletSpeed;
    public int damage;
    public int shotCount;
    public uint ticksBetweenShots;
    public float spreadRadius;
    public Entity attackPrefab;
}

public struct PendingShootElement : IBufferElementData
{
    public NetworkTick spawnTick;
}

public struct Owner : IComponentData
{
    public Entity Value;
}

public struct MeleeAttackProperties : IComponentData
{
    public uint cooldownTickCount;
    public int damage;
    public float attackRange;
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

public struct MeleeAttackCooldown : ICommandData
{
    public NetworkTick Tick { get; set; }
    public NetworkTick value;
}

public struct DamageFlashTimer : IComponentData
{
    [GhostField] public float Value;
    [GhostField] public float maxDuration;
}

public struct DamageFlashVisualLink : IComponentData
{
    public Entity VisualEntity;
}

public struct DamageUIEvent : IComponentData
{
    public float3 WorldPosition;
    public int DamageAmount;
}
public struct DamageNumberRpc : IRpcCommand
{
    public float3 Position;
    public int DamageAmount;
}

public struct AreaAttackProperties : IComponentData
{
    public Entity attack;
    public int dmgPerTick;
    public float timeToDmg;
    public float dmgInterval;
    public float TimeToAttack;
    public float aggroDistance;
    public float areaDuration;
    public float timeToDontMove;
    public float radiusArea;
}
public struct CooldownAreaAttack : IComponentData
{
    public NetworkTick NextPhaseTick;
    public NetworkTick NextDamageTick;
}

public struct AreaDamage : IComponentData
{
    public int dmgPerTick;
    public float timeToDmg;
    public float dmgInterval;
    public float radius;
    public Entity start;
    public Entity middle;
    public Entity end;
}
public enum AreaPhase : byte
{
    Preparing = 0, // Carregando (mostra o 'start')
    Impacting = 1  // Causando dano (mostra o 'end')
}

// Adicione isso no seu Baker junto com o AreaDamage!
public struct AreaDamageTimer : IComponentData
{
    public AreaPhase CurrentPhase;
    public NetworkTick NextPhaseTick; // O Tick em que a fase atual acaba
    public NetworkTick NextDamageTick; // Opcional: Se for causar dano por segundo durante o impacto
}
[GhostComponent]
public struct AreaVisualState : IComponentData
{
    [GhostField]
    public bool IsImpacting;
}

public struct AreaSlow : IComponentData
{
    public float slowAmount;
    public float duration;
}
public struct DurationAreaSlow : IComponentData
{
    public NetworkTick tick;
}
public struct SpawnOnDeath : IComponentData
{
    public Entity entity;
    public float scale;
}