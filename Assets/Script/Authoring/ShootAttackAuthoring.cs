using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class ShootAttackAuthoring : MonoBehaviour
{
    public float3 firePointOffset;
    public float attackCooldownTime;
    public GameObject attackPrefab;
    public float timeToDontMove;
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
            });
            AddBuffer<ShootAttackCooldown>(entity);
            AddBuffer<DontMoveOnTimer>(entity);
        }
    }
}
