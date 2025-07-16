using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EnemyMovementAuthoring : MonoBehaviour
{
    public float moveSpeed;

    public class Baker : Baker<EnemyMovementAuthoring>
    {
        public override void Bake(EnemyMovementAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Movement { });
            AddComponent(entity, new MoveSpeed { value = authoring.moveSpeed });
            AddComponent(entity, new Direction { });
        }
    }
}