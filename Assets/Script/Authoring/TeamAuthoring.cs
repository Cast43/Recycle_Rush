using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class TeamAuthoring : MonoBehaviour
{
    public Faction faction;
    public class Baker : Baker<TeamAuthoring>
    {
        public override void Bake(TeamAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Team { faction = authoring.faction });
        }
    }
}
