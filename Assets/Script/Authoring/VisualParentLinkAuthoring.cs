using Unity.Entities;
using UnityEngine;

// Arraste isso para o PREFAB do efeito
public class VisualParentLinkAuthoring : MonoBehaviour
{
    // Bake vazio ou com valor default

    class Baker : Baker<VisualParentLinkAuthoring>
    {
        public override void Bake(VisualParentLinkAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent<VisualParentLink>(entity);
        }
    }
}