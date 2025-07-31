using Unity.Entities;
using UnityEngine;
using Unity.Rendering;
using Unity.Mathematics;
using Unity.NetCode;

public class PlayerInputAuthoring : MonoBehaviour
{
    public class Baker : Baker<PlayerInputAuthoring>
    {
        public override void Bake(PlayerInputAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new PlayerInput());
        }
    }
}

[GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
public struct PlayerInput : IInputComponentData
{
    public InputEvent shoot; //pensar sobre isso
    public InputEvent dash;
}
