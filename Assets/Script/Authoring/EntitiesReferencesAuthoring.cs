using Unity.Entities;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

[System.Serializable]
public struct EnemysPrefab
{
    public string name;
    public GameObject prefab;
}

// Buffer element para inimigos
[InternalBufferCapacity(4)]
public struct EnemyPrefabElement : IBufferElementData
{
    public FixedString64Bytes name;
    public Entity prefab;
}
public class EntitiesReferencesAuthoring : MonoBehaviour
{
    [Header("Jogadores")]
    public GameObject playerRPrefab;
    public GameObject playerBPrefab;
    public GameObject playerYPrefab;
    public GameObject playerGPrefab;

    [Header("Inimigos")]
    public List<EnemysPrefab> enemyPrefabs;

    public class Baker : Baker<EntitiesReferencesAuthoring>
    {
        public override void Bake(EntitiesReferencesAuthoring authoring)
        {
            Entity entity = GetEntity(TransformUsageFlags.Dynamic);

            // Adiciona os jogadores como componente normal
            AddComponent(entity, new EntitiesReferences
            {
                playerRPrefab = GetEntity(authoring.playerRPrefab, TransformUsageFlags.Dynamic),
                playerBPrefab = GetEntity(authoring.playerBPrefab, TransformUsageFlags.Dynamic),
                playerYPrefab = GetEntity(authoring.playerYPrefab, TransformUsageFlags.Dynamic),
                playerGPrefab = GetEntity(authoring.playerGPrefab, TransformUsageFlags.Dynamic),
            });

            // Adiciona buffer de inimigos
            var enemyBuffer = AddBuffer<EnemyPrefabElement>(entity);
            foreach (var named in authoring.enemyPrefabs)
            {
                enemyBuffer.Add(new EnemyPrefabElement
                {
                    name = new FixedString64Bytes(named.name),
                    prefab = GetEntity(named.prefab, TransformUsageFlags.Dynamic)
                });
            }
        }
    }
}

// Só os jogadores como IComponentData
public struct EntitiesReferences : IComponentData
{
    public Entity playerRPrefab;
    public Entity playerBPrefab;
    public Entity playerYPrefab;
    public Entity playerGPrefab;
}
