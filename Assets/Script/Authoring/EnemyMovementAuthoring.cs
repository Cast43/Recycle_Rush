using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EnemyMovementAuthoring : MonoBehaviour
{
    public float maxSpeed;
    public float currentSpeed;

    public class Baker : Baker<EnemyMovementAuthoring>
    {
        public override void Bake(EnemyMovementAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Movement { });
            AddComponent(entity, new MoveSpeed
            {
                currentSpeed = authoring.maxSpeed,
                maxSpeed = authoring.currentSpeed
            });
            AddComponent(entity, new Direction { });
            AddBuffer<DontMoveOnTimer>(entity);
        }
    }
}