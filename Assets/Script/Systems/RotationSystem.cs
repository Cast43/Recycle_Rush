using UnityEngine;
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using Unity.Mathematics;
using Unity.Collections;

partial struct RotationSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // 1. Pegamos o DeltaTime para garantir velocidade constante independente do FPS
        NetworkTime networkTime = SystemAPI.GetSingleton<NetworkTime>();

        ObjectRotationJob objectRotationJob = new ObjectRotationJob
        {
            deltaTime = SystemAPI.Time.DeltaTime
        };
        state.Dependency = objectRotationJob.ScheduleParallel(state.Dependency);
    }

    [WithAll(typeof(GiveExperience))]
    public partial struct ObjectRotationJob : IJobEntity
    {
        public float deltaTime;

        public void Execute(in Rotation rotation, ref LocalTransform localTransform)
        {
            // 2. Definimos o quanto vamos girar NESTE frame
            // Velocidade (radianos/segundo) * tempo que passou
            float rotationAmount = rotation.rotationSpeed * deltaTime;

            // 3. Criamos um quaternion que representa "girar X graus no eixo Y"
            quaternion rotationStep = quaternion.RotateY(rotationAmount);

            // 4. Aplicamos essa rotação à rotação atual
            // A ordem da multiplicação importa!
            // math.mul(localTransform.ValueRO.Rotation, rotationStep) -> Gira no eixo LOCAL (do objeto)
            // math.mul(rotationStep, localTransform.ValueRO.Rotation) -> Gira no eixo GLOBAL (do mundo)

            // Aqui estou usando rotação Local (geralmente o desejado para objetos girando)
            localTransform.Rotation = math.mul(localTransform.Rotation, rotationStep);
        }
    }
}
