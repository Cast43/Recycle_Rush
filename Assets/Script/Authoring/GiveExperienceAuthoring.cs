using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class GiveExperienceAuthoting : MonoBehaviour
{
    public int giveExperience;
    public class Baker : Baker<GiveExperienceAuthoting>
    {
        public override void Bake(GiveExperienceAuthoting authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GiveExperience { value = authoring.giveExperience });
        }
    }
}