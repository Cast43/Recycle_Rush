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
    public int value;
}

public struct LevelModifier : IBufferElementData
{
    public UpgradeModifier Type;
    public float Value;
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
    [GhostField] public int Count;
}
public struct LevelUpTag : IComponentData { }
public struct GiveExperience : IComponentData
{
    [GhostField] public int value;
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