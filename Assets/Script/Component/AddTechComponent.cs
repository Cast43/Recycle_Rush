using Unity.NetCode;
using Unity.Entities;
using Unity.Collections;

public struct Tech : IBufferElementData
{
    public UpgradeModifier Type;
    public float amount;
    public float modifier;
    public float distance;
    public float maxDistance;
    public float cooldown;
}
public struct AddTech : IComponentData { };

