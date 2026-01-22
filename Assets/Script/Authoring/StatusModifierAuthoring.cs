using Unity.Entities;
using UnityEngine;
using Unity.Mathematics;
using Unity.NetCode;

public class StatusModifierAuthoring : MonoBehaviour
{

    public ModifierEntry[] modifiers;

    [System.Serializable]
    public struct ModifierEntry
    {
        public UpgradeModifier type;
        public float value;
    }
    public class Baker : Baker<StatusModifierAuthoring>
    {
        public override void Bake(StatusModifierAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Create DynamicBuffer for LevelModifier
            var buffer = AddBuffer<StatusModifier>(entity);
            foreach (var entry in authoring.modifiers)
            {
                //modificadores que ao passar de level aumentam alguma variável
                buffer.Add(new StatusModifier { Type = entry.type, Value = entry.value });
            }
        }
    }
}