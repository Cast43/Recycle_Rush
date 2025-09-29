using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
public partial struct CalculateFrameDamageSystem : ISystem
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

        foreach (var (damageBuffer, damageThisTickBuffer, entity) in
                SystemAPI.Query<DynamicBuffer<DamageBufferElement>, DynamicBuffer<DamageThisTick>>().WithAll<Simulate>().WithEntityAccess())
        {
            if (damageBuffer.IsEmpty)
            {
                damageThisTickBuffer.AddCommandData(new DamageThisTick { Tick = currentTick, value = 0, owner = Entity.Null });
            }
            else
            {
                int totalDamage = 0;
                Entity GetXP = Entity.Null;
                if (damageThisTickBuffer.GetDataAtTick(currentTick, out var damageThisTick))
                {
                    totalDamage = damageThisTick.value;
                }
                foreach (var damage in damageBuffer)
                {
                    totalDamage += damage.value;
                    GetXP = damage.owner;
                }
                // Debug.Log("A entidade " + entity + " recebeu " + totalDamage + " de dano");
                damageThisTickBuffer.AddCommandData(new DamageThisTick { Tick = currentTick, value = totalDamage, owner = GetXP });
                damageBuffer.Clear();
            }
        }
    }
}
