using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class DestroyOnTimerAuthoring : MonoBehaviour
{
    public float destroyOnTimer;
    public class Baker : Baker<DestroyOnTimerAuthoring>
    {
        public override void Bake(DestroyOnTimerAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DestroyOnTimer { value = authoring.destroyOnTimer });
        }
    }
}
