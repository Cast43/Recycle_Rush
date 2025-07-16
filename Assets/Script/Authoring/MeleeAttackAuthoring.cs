using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;
public class MeleeAttackAuthoring : MonoBehaviour
{
    public float attackCooldownTime;
    public int damage;
    public NetCodeConfig netCodeConfig;
    public int simulationTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;
    public class Baker : Baker<MeleeAttackAuthoring>
    {
        public override void Bake(MeleeAttackAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MeleeAttackProperties
            {
                cooldownTickCount = (uint)(authoring.attackCooldownTime * authoring.simulationTickRate),
                damage = authoring.damage,
            });
            AddBuffer<MeleeAttackCooldown>(entity);
            AddBuffer<AlreadyDamagedEntity>(entity);
        }
    }
}
