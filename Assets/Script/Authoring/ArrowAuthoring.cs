using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class ArrowAuthoring : MonoBehaviour
{
    public float moveSpeed;
    public Faction ally;

    public class Baker : Baker<ArrowAuthoring>
    {
        public override void Bake(ArrowAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Arrow
            {
                moveSpeed = authoring.moveSpeed,
            });
            AddComponent(entity, new Direction { lookDirection = float3.zero }); // Inicializa a direção
            AddComponent(entity, new Team { faction = authoring.ally }); // Inicializa a direção
            AddComponent(entity, new Owner { Value = entity }); // Inicializa a direção
        }
    }
}

public struct Arrow : IComponentData
{
    public float moveSpeed;
}