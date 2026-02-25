using Unity.Entities;
using UnityEngine;

public class DamageFlashAuthoring : MonoBehaviour
{
    public GameObject meshFilho;
    public float flashDuration;

    class Baker : Baker<DamageFlashAuthoring>
    {
        public override void Bake(DamageFlashAuthoring authoring)
        {
            if (authoring.meshFilho == null) return;

            var entidadePai = GetEntity(TransformUsageFlags.Dynamic);
            var entidadeVisual = GetEntity(authoring.meshFilho, TransformUsageFlags.Dynamic);

            // O Pai fica APENAS com o Timer e a Referência para o filho
            AddComponent(entidadePai, new DamageFlashTimer
            {
                Value = 0f,
                maxDuration = authoring.flashDuration
            });
            AddComponent(entidadePai, new DamageFlashVisualLink
            {
                VisualEntity = entidadeVisual
            });
        }
    }
}