using Unity.NetCode;
using Unity.Entities;
using Unity.Collections;

public struct ShowAddEffectRPC : IRpcCommand
{
    public int ClientNetId;
}
public struct AddEffectRpc : IRpcCommand
{
    public FixedString64Bytes EffectName;
}
public struct RequestChooseEffect : IComponentData { }
