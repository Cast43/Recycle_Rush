using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct CalculateFrameExperienceSystem : ISystem
{
    // [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<NetworkTime>();
    }

    // [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        NetworkTick currentTick = SystemAPI.GetSingleton<NetworkTime>().ServerTick;

        foreach (var (experienceBuffer, getExpThisTickBuffer, entity) in
                SystemAPI.Query<DynamicBuffer<ExperienceBufferElement>, DynamicBuffer<GetExperienceThisTick>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (experienceBuffer.IsEmpty)
            {
                getExpThisTickBuffer.AddCommandData(new GetExperienceThisTick { Tick = currentTick, value = 0 });
            }
            else
            {
                int totalExperience = 0;
                if (getExpThisTickBuffer.GetDataAtTick(currentTick, out var getExperienceThisTick))
                {
                    totalExperience = getExperienceThisTick.value;
                }
                foreach (var experience in experienceBuffer)
                {
                    totalExperience += experience.value;
                }
                // Debug.Log("o jogador " + entity + " ganhou " + totalExperience + " de Xp");
                getExpThisTickBuffer.AddCommandData(new GetExperienceThisTick { Tick = currentTick, value = totalExperience });
                experienceBuffer.Clear();
            }
        }
    }
}
