using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class PlayerRessurectAuthoring : MonoBehaviour
{
    public GameObject ressurectArea;

    public class Baker : Baker<PlayerRessurectAuthoring>
    {
        public override void Bake(PlayerRessurectAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new RessurectArea
            {
                ressurectionArea = GetEntity(authoring.ressurectArea, TransformUsageFlags.Dynamic),
            });
        }
    }
}