using Unity.Entities;
using Unity.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClientVisualEffectSystem : MonoBehaviour
{
    private EntityQuery _lightningQuery;
    private EntityQuery _poisonQuery;
    private EntityManager _entityManager;
    [SerializeField] private GameObject lightningEffect;
    [SerializeField] private GameObject poisonEffect;

    [System.Serializable]
    public class EffectReference
    {
        public GameObject effectGO;
        public Entity poisonEntity;
        public float duration;
    }

    public List<EffectReference> GoEffects;

    void Start()
    {
        // Pega o mundo ativo (Client ou Server)
        var world = World.DefaultGameObjectInjectionWorld;
        _entityManager = world.EntityManager;

        // Cria a query para buscar entidades com a sua Tag e o Buffer
        _lightningQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<LightningVisualTag>(),
            ComponentType.ReadOnly<LightningChainInfo>()
        );
        _poisonQuery = _entityManager.CreateEntityQuery(
            ComponentType.ReadOnly<PoisonVisualTag>(),
            ComponentType.ReadOnly<PoisonDuration>()
        );
    }

    void Update()
    {
        using var lightningEntities = _lightningQuery.ToEntityArray(Allocator.Temp);
        //Lightning
        foreach (var entity in lightningEntities)
        {
            var lightningBuffer = _entityManager.GetBuffer<LightningChainInfo>(entity);

            SpawnLightning(lightningBuffer);
        }

        //Poison
        using var poisonEntities = _poisonQuery.ToEntityArray(Allocator.Temp);
        foreach (var entity in poisonEntities)
        {
            var poisonComponent = _entityManager.GetComponentData<PoisonVisualTag>(entity);
            var poisonDurationBuff = _entityManager.GetBuffer<PoisonDuration>(entity);

            if (UniquePoisonEffect(entity))
                SpawnPoison(poisonComponent.position, poisonDurationBuff[0].radius, poisonDurationBuff[0].duration, entity);
        }
        DecreasePoisonTime();

    }
    void SpawnLightning(DynamicBuffer<LightningChainInfo> points)
    {
        // if (points.Length == 0) return;

        GameObject lightningTrail = Instantiate(lightningEffect);
        LineRenderer lightningLine = lightningTrail.GetComponent<LineRenderer>();

        lightningLine.positionCount = points.Length;

        Vector3[] positions = new Vector3[points.Length];

        for (int i = 0; i < points.Length; i++)
        {
            positions[i] = points[i].position;
            positions[i].y = 0.5f;
        }

        lightningLine.SetPositions(positions);

        Destroy(lightningTrail, 0.15f);
    }
    bool UniquePoisonEffect(Entity PoisonEntity)
    {
        foreach (var effect in GoEffects)
        {
            if (PoisonEntity == effect.poisonEntity)
            {
                return false;
            }
        }
        return true;
    }
    void SpawnPoison(Vector3 position, float scale, float duration, Entity entityReference)
    {
        GameObject poison = Instantiate(poisonEffect, position, Quaternion.identity);
        poison.transform.localScale = new Vector3(scale, scale, scale);
        EffectReference reference = new EffectReference();
        reference.effectGO = poison;
        reference.duration = duration;
        reference.poisonEntity = entityReference;
        GoEffects.Add(reference);
    }
    void DecreasePoisonTime()
    {
        for (int i = GoEffects.Count - 1; i >= 0; i--)
        {
            var effect = GoEffects[i];
            if (!_entityManager.Exists(effect.poisonEntity))
            {
                Destroy(effect.effectGO);
                GoEffects.RemoveAt(i);
            }
        }
    }
}