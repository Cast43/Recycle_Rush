using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class GetExperienceInAreaAuthoting : MonoBehaviour
{
    public float radius;
    public class Baker : Baker<GetExperienceInAreaAuthoting>
    {
        public override void Bake(GetExperienceInAreaAuthoting authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GetExperienceInArea { radius = authoring.radius });
            AddBuffer<AlreadyGiveExperienceEntity>(entity);
        }
    }
}