using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class DropExperienceAuthoring : MonoBehaviour
{
    public GameObject[] experiencePrefabs;
    public class Baker : Baker<DropExperienceAuthoring>
    {
        public override void Bake(DropExperienceAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            var experienceBuffer = AddBuffer<DropExperienceEntity>(entity);
            foreach (var enemyPrefab in authoring.experiencePrefabs)
            {
                experienceBuffer.Add(new DropExperienceEntity
                {
                    value = GetEntity(enemyPrefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}
