using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EnemyAuthoting : MonoBehaviour
{
    public int giveExperience;
    public class Baker : Baker<EnemyAuthoting>
    {
        public override void Bake(EnemyAuthoting authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Enemy());
            AddComponent(entity, new GiveExperience { value = authoring.giveExperience });
        }
    }
}

public struct Enemy : IInputComponentData
{

}