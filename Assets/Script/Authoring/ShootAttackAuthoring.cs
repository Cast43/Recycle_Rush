using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class ShootAttackAuthoring : MonoBehaviour
{
    public float3 firePointOffset = new float3(0, 0.5f, 0);
    public float attackCooldownTime = 6;
    public GameObject attackPrefab;
    public float timeToDontMove = 1;
    public float bulletLifeTime = 4;
    public float bulletSpeed = 4;
    public int damage = 1;
    public int lostEnergy = 4;
    public int shotCount = 1;
    public uint ticksBetweenShots;
    public float spreadRadius = 0.2f;
    public NetCodeConfig netCodeConfig;
    public int simulationTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;

    public class Baker : Baker<ShootAttackAuthoring>
    {
        public override void Bake(ShootAttackAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ShootAttackProperties
            {
                firePointOffset = authoring.firePointOffset,
                cooldownTickCount = (uint)(authoring.attackCooldownTime * authoring.simulationTickRate),
                attackPrefab = GetEntity(authoring.attackPrefab, TransformUsageFlags.Dynamic),
                timeToDontMove = (uint)(authoring.timeToDontMove * authoring.simulationTickRate),
                bulletLifeTime = (uint)(authoring.bulletLifeTime),
                bulletSpeed = authoring.bulletSpeed,
                damage = authoring.damage,
                lostEnergy = (uint)authoring.lostEnergy,
                shotCount = authoring.shotCount,
                ticksBetweenShots = authoring.ticksBetweenShots,
                spreadRadius = authoring.spreadRadius,
            });

            AddBuffer<PendingShootElement>(entity);
            AddBuffer<ShootAttackCooldown>(entity);
            // AddBuffer<DontMoveOnTimer>(entity);
        }
    }
}
