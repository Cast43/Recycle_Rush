using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class StructuresAuthoring : MonoBehaviour
{
    public Faction faction;
    public class Baker : Baker<StructuresAuthoring>
    {
        public override void Bake(StructuresAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Team { faction = authoring.faction });
        }
    }
}
