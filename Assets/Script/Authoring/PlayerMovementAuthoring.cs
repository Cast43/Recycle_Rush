using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

public class PlayerMovementAuthoring : MonoBehaviour
{
    public float maxSpeed;
    public float currentSpeed;
    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;
    public int dashEnergyCost;
    public class Baker : Baker<PlayerMovementAuthoring>
    {
        public override void Bake(PlayerMovementAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MovementPlayer { });
            AddComponent(entity, new MoveSpeed
            {
                currentSpeed = authoring.maxSpeed,
                maxSpeed = authoring.currentSpeed
            });
            AddComponent(entity, new Direction { });
            AddComponent(entity, new DashProperties
            {
                lostEnergy = authoring.dashEnergyCost,
                duration = (authoring.dashDuration),
                cooldown = (authoring.dashCooldown),
                speed = authoring.dashSpeed,
                canDash = true,
                isDashing = false,
            });
            AddBuffer<DashCommand>(entity);
            AddBuffer<DashCooldown>(entity);
            AddBuffer<DashDuration>(entity);
            AddBuffer<DontMoveOnTimer>(entity);
        }
    }
}