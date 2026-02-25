using Unity.Entities;
using Unity.Mathematics;
using Unity.Rendering;
using UnityEngine;

public class DamageFlashVisualAuthoring : MonoBehaviour
{
    class Baker : Baker<DamageFlashVisualAuthoring>
    {
        public override void Bake(DamageFlashVisualAuthoring authoring)
        {
            var entidadeVisual = GetEntity(TransformUsageFlags.Dynamic);

            // O Filho cuida EXCLUSIVAMENTE de adicionar o componente de cor a si mesmo
            AddComponent(entidadeVisual, new URPMaterialPropertyBaseColor
            {
                Value = new float4(1, 1, 1, 1) // Inicia Branco
            });
        }
    }
}