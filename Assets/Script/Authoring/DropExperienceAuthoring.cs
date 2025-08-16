using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class DropExperienceAuthoring : MonoBehaviour
{
    public GameObject experience;
    public class Baker : Baker<DropExperienceAuthoring>
    {
        public override void Bake(DropExperienceAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new DropExperienceEntity { value = GetEntity(authoring.experience, TransformUsageFlags.None) });
        }
    }
}
