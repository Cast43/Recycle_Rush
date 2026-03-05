using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class LevelingAuthoring : MonoBehaviour
{
    public int maxExperience;
    public float maxExperienceModifier;
    public int startLevel = 1;
    public int MaxCapacityGarbage = 10;

    public ModifierEntry[] modifiers;

    [System.Serializable]
    public struct ModifierEntry
    {
        public UpgradeModifier type;
        public float value;
        public float divideWaveGain;
    }

    public int plasticCount = 1;
    public int paperCount = 1;
    public int glassCount = 1;
    public int metalCount = 1;
    public int organicCount = 1;
    public int notRecycleCount = 1;

    public class Baker : Baker<LevelingAuthoring>
    {
        public override void Bake(LevelingAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new MaxExperience
            {
                value = authoring.maxExperience,
                modier = authoring.maxExperienceModifier
            });
            AddComponent(entity, new Level { current = authoring.startLevel, previous = authoring.startLevel });
            AddComponent(entity, new CurrentExperience { value = 0 });
            AddComponent(entity, new GarbageInventory
            {
                MaxCapacityPerType = authoring.MaxCapacityGarbage,
                PlasticCount = authoring.plasticCount,
                PaperCount = authoring.paperCount,
                GlassCount = authoring.glassCount,
                MetalCount = authoring.metalCount,
                OrganicCount = authoring.organicCount,
                NotRecycleCount = authoring.notRecycleCount,
            });
            AddBuffer<UpgradesPending>(entity);
            AddBuffer<ExperienceBufferElement>(entity);
            AddBuffer<GetExperienceThisTick>(entity);
            // AddBuffer<LevelUpTag>(entity);

            // Create DynamicBuffer for LevelModifier
            var buffer = AddBuffer<LevelModifier>(entity);
            foreach (var entry in authoring.modifiers)
            {
                //modificadores que ao passar de level aumentam alguma variável
                buffer.Add(new LevelModifier
                {
                    Type = entry.type,
                    Value = entry.value,
                    divideWaveGain = entry.divideWaveGain
                });
            }
        }
    }
}