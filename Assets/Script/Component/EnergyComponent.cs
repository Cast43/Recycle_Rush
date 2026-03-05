using Unity.Entities;
using Unity.NetCode;
using Unity.Mathematics;


public struct MaxEnergy : IComponentData
{
    public int value;
}
public struct CurrentEnergy : IComponentData
{
    [GhostField] public int value;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct EnergyBufferElement : IBufferElementData
//tendo energia acumulada eu chamo o get energy no tick para reduzir current experience
{
    public int value;
}
[GhostComponent(PrefabType = GhostPrefabType.AllPredicted, OwnerSendType = SendToOwnerType.SendToNonOwner)]
public struct GetEnergyThisTick : ICommandData
//faz o calculo no tick de toda a experiencia
{
    public NetworkTick Tick { get; set; }
    public int value;
}
public struct EnergyRestore : IComponentData
{
    public int amount;
    public float cooldownRestore;
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct EnergyRestoreCooldown : IComponentData
{
    [GhostField]
    public NetworkTick value;
}
public struct EnergyRestoreMovement : IComponentData
{
    public float distance;
    public float maxDistance;
    public int amount;
}
public struct EnergyRestoreKill : IComponentData
{
    public int amount;
}
public struct GetEnergyFromKill : IBufferElementData
{
    public int amount;
}
