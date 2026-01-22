using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Mathematics;

public struct StatusModifier : IBufferElementData
{
    public UpgradeModifier Type;
    public float Value;
}
public struct UpdateStatus : IComponentData { };

