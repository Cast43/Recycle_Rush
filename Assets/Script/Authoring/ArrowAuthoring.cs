using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

public class ArrowAuthoring : MonoBehaviour
{
    public float moveSpeed;
    public Faction ally;
    public NetCodeConfig netCodeConfig;
    public int simulationTickRate => netCodeConfig.ClientServerTickRate.SimulationTickRate;
    public GameObject[] effectPrefabs; // arraste aqui seus prefabs Poison, Burn, Slow…

    public class Baker : Baker<ArrowAuthoring>
    {
        public override void Bake(ArrowAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new Arrow
            {
                moveSpeed = authoring.moveSpeed,
            });
            AddComponent(entity, new Direction { lookDirection = float3.zero }); // Inicializa a direção
            AddComponent(entity, new Team { faction = authoring.ally });
            AddComponent(entity, new Owner { Value = entity });

            var buffer = AddBuffer<EffectPrefab>(entity);
            foreach (var go in authoring.effectPrefabs)
            {
                buffer.Add(new EffectPrefab { Prefab = GetEntity(go, TransformUsageFlags.None) });
            }

        }
    }
}

public struct Arrow : IComponentData
{
    public float moveSpeed;
}