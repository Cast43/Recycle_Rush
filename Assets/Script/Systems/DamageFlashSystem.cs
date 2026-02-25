using Unity.Entities;
using Unity.Rendering;
using Unity.Mathematics;

// [UpdateInGroup(typeof(PresentationSystemGroup))]
public partial struct DamageFlashSystem : ISystem
{
    public void OnUpdate(ref SystemState state)
    {
        float deltaTime = SystemAPI.Time.DeltaTime;

        // Procura todos os pais que têm Timer e o Link visual
        foreach (var (timer, link) in SystemAPI.Query<RefRW<DamageFlashTimer>, RefRO<DamageFlashVisualLink>>())
        {
            // Pega o componente de cor diretamente da entidade filha
            if (!SystemAPI.HasComponent<URPMaterialPropertyBaseColor>(link.ValueRO.VisualEntity)) continue;

            var baseColor = SystemAPI.GetComponentRW<URPMaterialPropertyBaseColor>(link.ValueRO.VisualEntity);

            if (timer.ValueRO.Value > 0)
            {
                baseColor.ValueRW.Value = new float4(1, 0, 0, 1);
                timer.ValueRW.Value -= deltaTime;
            }
            else
            {
                baseColor.ValueRW.Value = new float4(1, 1, 1, 1);
            }
        }
    }
}