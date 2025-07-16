using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class RotationAuthoring : MonoBehaviour
{
    public float rotationSpeed;
    public float Yoffset;
    public class Baker : Baker<RotationAuthoring>
    {
        public override void Bake(RotationAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Rotation
            {
                rotationSpeed = authoring.rotationSpeed,
                Yoffset = authoring.Yoffset,
            });
        }
    }
}
