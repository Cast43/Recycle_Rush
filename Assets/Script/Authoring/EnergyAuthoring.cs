using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class EnergyAuthoting : MonoBehaviour
{
    public int maxEnergy;
    public int startEnergy = 100;
    public float energyRestoreCooldown = 1;
    public int energyRestoreAmount = 5;

    public class Baker : Baker<EnergyAuthoting>
    {
        public override void Bake(EnergyAuthoting authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MaxEnergy { value = authoring.maxEnergy });
            AddComponent(entity, new CurrentEnergy { value = authoring.startEnergy });
            AddComponent(entity, new EnergyRestore
            {
                amount = authoring.energyRestoreAmount,
                cooldownRestore = (uint)authoring.energyRestoreCooldown
            });
            AddComponent(entity, new EnergyRestoreCooldown { });
            AddBuffer<EnergyBufferElement>(entity);
            AddBuffer<GetEnergyThisTick>(entity);
        }
    }
}