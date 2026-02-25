using Unity.Entities;
using UnityEngine;

public class CoreAuthoring : MonoBehaviour
{

    class Baker : Baker<CoreAuthoring>
    {
        public override void Bake(CoreAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new GetCore { });
        }
    }
}