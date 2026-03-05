using Unity.Entities;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

public class EntitiesReferencesAuthoring : MonoBehaviour
{


    [Header("Jogadores")]
    public GameObject playerRPrefab;
    public GameObject playerBPrefab;
    public GameObject playerYPrefab;
    public GameObject playerGPrefab;

    [Header("Inimigos")]
    public GameObject[] enemyPrefabs;

    [Header("Bosses")]
    public GameObject[] bossesPrefabs;

    [Header("Experience")]
    public GameObject[] ExperiencePrefabs;

    [Header("Events")]
    public GameObject[] EventsPrefabs;

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
            foreach (var enemyPrefab in authoring.enemyPrefabs)
            {
                enemyBuffer.Add(new EnemyPrefabElement
                {
                    name = new FixedString64Bytes(enemyPrefab.name),
                    prefab = GetEntity(enemyPrefab, TransformUsageFlags.Dynamic)
                });
            }

            var bossesBuffer = AddBuffer<BossPrefabElement>(entity);
            foreach (var bossPrefab in authoring.bossesPrefabs)
            {
                bossesBuffer.Add(new BossPrefabElement
                {
                    name = new FixedString64Bytes(bossPrefab.name),
                    prefab = GetEntity(bossPrefab, TransformUsageFlags.Dynamic)
                });
            }
            var experienceBuffer = AddBuffer<ExperiencePrefabElement>(entity);
            foreach (var experiencePrefab in authoring.ExperiencePrefabs)
            {
                experienceBuffer.Add(new ExperiencePrefabElement
                {
                    Prefab = GetEntity(experiencePrefab, TransformUsageFlags.Dynamic)
                });
            }
            var eventsBuffer = AddBuffer<EventsPrefabElement>(entity);
            foreach (var eventsPrefabs in authoring.EventsPrefabs)
            {
                eventsBuffer.Add(new EventsPrefabElement
                {
                    Prefab = GetEntity(eventsPrefabs, TransformUsageFlags.Dynamic)
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
