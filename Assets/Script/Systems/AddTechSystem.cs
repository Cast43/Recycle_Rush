using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using Unity.Mathematics;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
partial struct AddTechSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {

    }

    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<Tech>>().WithAll<AddTech>().WithEntityAccess())
        {
            // i = level.ValueRO.current;
            // Apply each modifier according to current level
            foreach (var mod in buffer)
            {
                switch (mod.Type)
                {
                    case UpgradeModifier.AddEolicTurbines:
                        if (state.EntityManager.HasComponent<EnergyRestoreMovement>(entity))
                        {
                            var previousEolic = state.EntityManager.GetComponentData<EnergyRestoreMovement>(entity);
                            var eolicTurbine = new EnergyRestoreMovement
                            {
                                maxDistance = mod.modifier * previousEolic.maxDistance,
                                // distance = mod.ValueB+ previousEolic.distance,
                                amount = (int)math.round(previousEolic.amount + mod.modifier),
                            };
                            ECB.SetComponent<EnergyRestoreMovement>(entity, eolicTurbine);
                        }
                        else
                        {
                            var eolicTurbine = new EnergyRestoreMovement
                            {
                                distance = mod.distance,
                                maxDistance = mod.maxDistance,
                                amount = (int)mod.amount,
                            };
                            ECB.AddComponent<EnergyRestoreMovement>(entity, eolicTurbine);
                        }

                        break;
                    case UpgradeModifier.AddBiomassGenerator:
                        if (state.EntityManager.HasComponent<EnergyRestoreKill>(entity))
                        {
                            var previousBiomass = state.EntityManager.GetComponentData<EnergyRestoreKill>(entity);
                            var BiomassMotor = new EnergyRestoreKill
                            {
                                amount = (int)mod.modifier + previousBiomass.amount,
                            };
                            ECB.SetComponent<EnergyRestoreKill>(entity, BiomassMotor);
                        }
                        else
                        {
                            var BiomassMotor = new EnergyRestoreKill
                            {
                                amount = (int)mod.amount,
                            };
                            ECB.AddComponent<EnergyRestoreKill>(entity, BiomassMotor);
                        }

                        break;
                    case UpgradeModifier.AddPassiveEnergy:
                        if (state.EntityManager.HasComponent<EnergyRestore>(entity))
                        {
                            var previousSolar = state.EntityManager.GetComponentData<EnergyRestore>(entity);
                            float newCooldown = previousSolar.cooldownRestore * (1 - (mod.amount / 100));
                            var SolarEnergy = new EnergyRestore
                            {
                                amount = (int)mod.modifier + previousSolar.amount,
                                cooldownRestore = newCooldown
                            };
                            ECB.SetComponent<EnergyRestore>(entity, SolarEnergy);
                        }
                        else
                        {
                            var SolarEnergy = new EnergyRestore
                            {
                                amount = (int)mod.amount,
                                cooldownRestore = mod.cooldown
                            };
                            ECB.AddComponent<EnergyRestore>(entity, SolarEnergy);
                            ECB.AddBuffer<EnergyRestoreCooldown>(entity);
                        }
                        break;
                }
            }
            buffer.Clear();
            ECB.RemoveComponent<UpdateStatus>(entity);
        }
        ECB.Playback(state.EntityManager);
        ECB.Dispose();
    }
}
