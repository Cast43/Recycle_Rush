using Unity.Entities;
using UnityEngine;
using Unity.NetCode;

public class EnemyDashAuthoring : MonoBehaviour
{
    public float aggroDash;
    public float dashSpeed;
    public float dashDuration;
    public float dashCooldown;
    public class Baker : Baker<EnemyDashAuthoring>
    {
        public override void Bake(EnemyDashAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new EnemyDashProperties
            {
                aggroDistance = authoring.aggroDash,
                duration = (authoring.dashDuration),
                cooldown = (authoring.dashCooldown),
                speed = authoring.dashSpeed,
                isDashing = false,
            });
            AddBuffer<DashCooldown>(entity);
            AddBuffer<DashDuration>(entity);
        }
    }
}