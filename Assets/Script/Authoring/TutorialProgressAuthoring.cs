using Unity.Entities;
using UnityEngine;

public class TutorialProgressAuthoring : MonoBehaviour
{
    class Baker : Baker<TutorialProgressAuthoring>
    {
        public override void Bake(TutorialProgressAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.Dynamic);

            // É isso aqui que avisa o Netcode: "Ei, sincronize isso pela rede!"
            AddComponent(entity, new TutorialProgress { CurrentStep = 0, IsCompleted = false });
        }
    }
}