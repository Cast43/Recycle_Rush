using Unity.Entities;
using UnityEngine;

// 2. O Authoring (MonoBehaviour para o Inspector)
public class RecyclingBinAuthoring : MonoBehaviour
{
    public float Radius;
    public TrashType AcceptedType;

    public class Baker : Baker<RecyclingBinAuthoring>
    {
        public override void Bake(RecyclingBinAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            AddComponent(entity, new RecyclingBinData
            {
                Radius = authoring.Radius,
                AcceptedType = authoring.AcceptedType
            });
        }
    }
}