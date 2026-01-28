using Unity.Burst;
using Unity.Entities;
using Unity.Transforms;
using Unity.NetCode;
using UnityEngine;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Mathematics;
using Unity.Collections;


[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))] //acho que isso resolve o problema de pai
public partial struct MakeParentSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        EntityCommandBuffer ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);

        foreach (var (makeParent, localTransform, particle) in SystemAPI.Query<RefRO<VisualParentLink>, RefRW<LocalTransform>>().WithEntityAccess())
        {
            Entity desiredParent = makeParent.ValueRO.ParentEntity;
            Entity currentParent = Entity.Null;

            // Verifica se a entidade já tem um pai atribuído
            if (SystemAPI.HasComponent<Parent>(particle))
            {
                currentParent = SystemAPI.GetComponent<Parent>(particle).Value;
            }

            // Se o pai desejado for diferente do atual, precisamos agir
            if (desiredParent != currentParent)
            {
                // Verifica se a entidade pai existe NESTE mundo (Cliente ou Server)
                // Isso resolve o problema do "Ghost Lag" onde o efeito chega antes do pai
                if (state.EntityManager.Exists(desiredParent))
                {
                    // Aplica ou Atualiza o componente Parent
                    if (SystemAPI.HasComponent<Parent>(particle))
                    {
                        ECB.SetComponent(particle, new Parent { Value = desiredParent });
                    }
                    else
                    {
                        ECB.AddComponent(particle, new Parent { Value = desiredParent });
                    }

                    // Reseta a posição local para garantir que fique relativo ao pai corretamente
                    // (Você pode remover isso se quiser manter a posição global ao trocar de pai)
                    // localTransform.ValueRW.Position = new float3(0, 0.5f, 0); // Seu offset
                    // localTransform.ValueRW.Rotation = quaternion.identity;
                    // localTransform.ValueRW.Scale = 1.0f;
                    // ECB.DestroyEntity(particle);
                    // IMPORTANTE: LinkedEntityGroup para destruição em cadeia
                    // Se o pai for destruído, o efeito some junto? Se sim, precisamos adicionar ao Buffer do pai.
                    // Nota: Modificar buffers de outras entidades no Cliente pode ser arriscado se houver prediction,
                    // mas para efeitos visuais puros geralmente é aceitável ou desnecessário se o efeito tiver tempo de vida próprio.
                }
            }
        }
    }
}
