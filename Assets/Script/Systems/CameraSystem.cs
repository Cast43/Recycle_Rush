using Unity.Burst;
using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Physics;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
partial class CameraSystem : SystemBase
{
    // [BurstCompile]
    protected override void OnUpdate()
    {
        // Encontrar o jogador local
        Entity localPlayer = Entity.Null;

        foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
                                              .WithAll<GhostOwnerIsLocal>()
                                              .WithEntityAccess())
        {
            localPlayer = entity;
            break;
        }
        if (localPlayer == Entity.Null) return;

        LocalTransform playerPos = SystemAPI.GetComponent<LocalTransform>(localPlayer);

        // //se encontrou o player
        CameraSingleton camera = CameraSingleton.Instance;

        float3 desiredPosition = (float3)playerPos.Position + camera.offset;
        float distanceToCamera = math.distance(camera.transform.position, desiredPosition);

        // Calcula um smoothSpeed dinâmico com base na distância
        float dynamicSmooth = camera.smoothSpeed;
        if (distanceToCamera > camera.maxDist)
            dynamicSmooth *= 1.5f; // acelera se estiver muito longe
        else if (distanceToCamera < camera.minDist)
            dynamicSmooth *= 0.5f; // desacelera se estiver muito perto

        camera.transform.position = math.lerp(camera.transform.position, desiredPosition, dynamicSmooth * SystemAPI.Time.DeltaTime);



        // Entity playerLocal = Entity.Null;
        // float3 playerPosition = float3.zero;

        // foreach (var (transform, entity) in SystemAPI.Query<RefRO<LocalTransform>>()
        //                                               .WithAll<GhostOwnerIsLocal>()
        //                                               .WithEntityAccess())
        // {

        //     playerLocal = entity;
        //     playerPosition = transform.ValueRO.Position;
        //     break; // Só precisa de um (o jogador local)
        // }

        // // Se não encontrou o jogador local, sair
        // if (playerLocal == Entity.Null)
        //     return;

        // //se encontrou o player
        // foreach ((RefRW<LocalTransform> localTransform, RefRO<CameraFollow> cameraFollow)
        //         in SystemAPI.Query<RefRW<LocalTransform>, RefRO<CameraFollow>>())
        // {
        //     float3 desiredPosition = (float3)playerPosition + cameraFollow.ValueRO.offset;
        //     float distanceToCamera = math.distance(localTransform.ValueRO.Position, desiredPosition);

        //     // Calcula um smoothSpeed dinâmico com base na distância
        //     float dynamicSmooth = cameraFollow.ValueRO.smoothSpeed;
        //     if (distanceToCamera > cameraFollow.ValueRO.maxDist)
        //         dynamicSmooth *= 1.5f; // acelera se estiver muito longe
        //     else if (distanceToCamera < cameraFollow.ValueRO.minDist)
        //         dynamicSmooth *= 0.5f; // desacelera se estiver muito perto

        //     localTransform.ValueRW.Position = math.lerp(localTransform.ValueRO.Position, desiredPosition, dynamicSmooth * SystemAPI.Time.DeltaTime);
        // }
    }
}
