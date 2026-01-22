using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;
public class AddTechAuthoring : MonoBehaviour
{
    public TechEntry[] techs;

    [System.Serializable]
    public struct TechEntry
    {
        public UpgradeModifier type;
        public float amount;
        public float modifier;
        public float distance;
        public float maxDistance;
        public float cooldown;
    }
    public class Baker : Baker<AddTechAuthoring>
    {
        public override void Bake(AddTechAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Create DynamicBuffer for LevelModifier
            var buffer = AddBuffer<Tech>(entity);
            foreach (var entry in authoring.techs)
            {
                //modificadores que ao passar de level aumentam alguma variável
                buffer.Add(new Tech
                {
                    Type = entry.type,
                    amount = entry.amount,
                    modifier = entry.modifier,
                    distance = entry.distance,
                    maxDistance = entry.maxDistance,
                    cooldown = entry.cooldown
                });
            }
        }
    }
}
