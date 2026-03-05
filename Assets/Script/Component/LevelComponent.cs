using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Mathematics;


public struct DropExperienceEntity : IBufferElementData
{
    public Entity value;
}

public struct AlreadySpawnedXPTag : IComponentData { }
public struct GetExperienceInArea : IComponentData
{
    [GhostField] public float radius;
}

public struct MaxExperience : IComponentData
{
    [GhostField] public int value;
    public float modier;
}

public struct LevelModifier : IBufferElementData
{
    public UpgradeModifier Type;
    public float Value;
    public float divideWaveGain;
}
public struct Level : IComponentData
{
    [GhostField] public int current;
    [GhostField] public int previous;
    // [GhostField] public bool leveling;
}
public struct CurrentExperience : IComponentData
{
    [GhostField] public int value;
}
public struct UpgradesPending : IBufferElementData
{
    [GhostField] public UpgradeLevel upgradeLevel;
}
public struct LevelUpTag : IComponentData { }
public struct GiveExperience : IComponentData
{
    [GhostField] public int value;
    [GhostField] public TrashType tarshType;
}
public struct AlreadyGiveExperienceEntity : IBufferElementData
{
    public Entity value;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct ExperienceBufferElement : IBufferElementData
//tendo experiencia acumulada eu chamo o get experience no tick
{
    public int value;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct GetExperienceThisTick : ICommandData
//faz o calculo no tick de toda a experiencia
{
    public NetworkTick Tick { get; set; }
    public int value;
}

public struct RandomSpawnExperience : IComponentData
{
    public float cooldown;
    public int maxExperienceSpawned;
    public int countExperienceSpawned;
    public float3 spawnPosition;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct RandomSpawnExperienceCooldown : IComponentData
{
    [GhostField]
    public NetworkTick value;
}
public struct ExperienceGlobalRefence : IBufferElementData
{
    public Entity value;
}

[InternalBufferCapacity(8)]
public struct ExperiencePrefabElement : IBufferElementData
{
    public Entity Prefab;
}
public enum TrashType : byte { Plastic, Paper, Glass, Iron, Organic, NotRecycle }
public struct GarbageInventory : IComponentData
{
    // Quantidade atual de cada tipo
    [GhostField] public int PlasticCount;
    [GhostField] public int PaperCount;
    [GhostField] public int GlassCount;
    [GhostField] public int MetalCount;
    [GhostField] public int OrganicCount;
    [GhostField] public int NotRecycleCount;

    // O limite máximo que o robô pode carregar de CADA tipo (ex: 10)
    [GhostField] public int MaxCapacityPerType;
    [GhostField] public int GarbageCount;
}

// 1. O Componente de Dados puro (ECS)
public struct RecyclingBinData : IComponentData
{
    public float Radius;
    public TrashType AcceptedType;
}
