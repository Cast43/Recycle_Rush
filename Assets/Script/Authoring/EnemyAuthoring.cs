using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EnemyAuthoting : MonoBehaviour
{
    public class Baker : Baker<EnemyAuthoting>
    {
        public override void Bake(EnemyAuthoting authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Enemy());
        }
    }
}

public struct Enemy : IComponentData
{

}