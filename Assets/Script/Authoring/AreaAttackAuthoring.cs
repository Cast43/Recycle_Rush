using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class AreaAttackAuthoring : MonoBehaviour
{
    public GameObject areaPrefab;
    public float areaDuration = 4f;
    public int dmgPerTick = 1;
    public float timeToDmg = 2f;
    public float dmgInterval = 2f;
    public float timeToAttack = 2f;
    public float aggroDistance = 6;
    public float timeToDontMove = 0.5f;
    public float radiusArea = 5f;
    public class Baker : Baker<AreaAttackAuthoring>
    {
        public override void Bake(AreaAttackAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new AreaAttackProperties
            {
                attack = GetEntity(authoring.areaPrefab, TransformUsageFlags.None),
                dmgPerTick = authoring.dmgPerTick,
                timeToDmg = authoring.timeToDmg,
                dmgInterval = authoring.dmgInterval,
                radiusArea = authoring.radiusArea,
                TimeToAttack = authoring.timeToAttack,
                aggroDistance = authoring.aggroDistance,
                timeToDontMove = authoring.timeToDontMove,
                areaDuration = authoring.areaDuration,
            });
            AddComponent<CooldownAreaAttack>(entity);

        }
    }
}