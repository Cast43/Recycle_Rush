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

        // 1. PASSO GLOBAL: Somar a experiência coletada por TODOS os jogadores no frame
        int totalGlobalExperienceThisFrame = 0;
        foreach (var (experienceBuffer, AlreadyGiveExperience, getExpThisTickBuffer) in 
                SystemAPI.Query<DynamicBuffer<ExperienceBufferElement>, DynamicBuffer<AlreadyGiveExperienceEntity>, DynamicBuffer<GetExperienceThisTick>>().WithAll<Simulate>())
        {
            foreach (var experience in experienceBuffer)
            {
                totalGlobalExperienceThisFrame += experience.value;
            }
            
            experienceBuffer.Clear();
        }

        // 2. PASSO INDIVIDUAL: Distribuir o montante global para todos e limpar os buffers
        foreach (var (experienceBuffer, AlreadyGiveExperience, getExpThisTickBuffer, entity) in
                SystemAPI.Query<DynamicBuffer<ExperienceBufferElement>, DynamicBuffer<AlreadyGiveExperienceEntity>, DynamicBuffer<GetExperienceThisTick>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (totalGlobalExperienceThisFrame == 0)
            {
                getExpThisTickBuffer.AddCommandData(new GetExperienceThisTick { Tick = currentTick, value = 0 });
                AlreadyGiveExperience.Clear();
            }
            else
            {
                int totalExperience = 0;
                if (getExpThisTickBuffer.GetDataAtTick(currentTick, out var getExperienceThisTick))
                {
                    totalExperience = getExperienceThisTick.value;
                }

                // Adiciona o bolão global da equipe na experiência deste jogador
                totalExperience += totalGlobalExperienceThisFrame;

                getExpThisTickBuffer.AddCommandData(new GetExperienceThisTick { Tick = currentTick, value = totalExperience });
                AlreadyGiveExperience.Clear();
            }
        }
    }
}
