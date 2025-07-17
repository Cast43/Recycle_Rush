using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class PlayerMovementAuthoring : MonoBehaviour
{
    public float moveSpeed;
    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;
    public NetCodeConfig netCodeConfig;
    public int simulationTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;
    public class Baker : Baker<PlayerMovementAuthoring>
    {
        public override void Bake(PlayerMovementAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MovementPlayer { });
            AddComponent(entity, new MoveSpeed { value = authoring.moveSpeed });
            AddComponent(entity, new Direction { });
            AddComponent(entity, new DashProperties
            {
                duration = (uint)(authoring.dashDuration * authoring.simulationTickRate),
                cooldown = (uint)(authoring.dashCooldown * authoring.simulationTickRate),
                speed = authoring.dashSpeed,
                canDash = true,
                isDashing = false,
            });
            AddComponent(entity, new DashVector { });
            AddBuffer<DashCooldown>(entity);
            AddBuffer<DashDuration>(entity);
        }
    }
}