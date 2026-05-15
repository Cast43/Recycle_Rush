using Unity.NetCode;
using Unity.Entities;
using Unity.Collections;

public struct GlobalUpgradesPrefab : IBufferElementData
{
    [GhostField]
    public Entity Prefab;
    [GhostField]
    public FixedString64Bytes Name;
}
public struct ShowUpgradesRPC : IRpcCommand
{
    public int ClientNetId;
    public UpgradeLevel upgradeLevel;
}
public struct AddEffectRpc : IRpcCommand
{
    public FixedString64Bytes EffectName;
}
public struct ModifierStatusRpc : IRpcCommand
{
    public FixedString64Bytes ModifierName;
}
public struct AddTechRpc : IRpcCommand
{
    public FixedString64Bytes ComponentName;
}
public struct DecreaseUpgradesPendingRpc : IRpcCommand { }
public struct GetCore : IComponentData { }
