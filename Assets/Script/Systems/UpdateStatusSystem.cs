using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameExperienceSystem))]
partial struct UpdateStatusSystem : ISystem
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public void OnCreate(ref SystemState state)
    {

    }

    // Update is called once per frame
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<StatusModifier>>().WithAll<UpdateStatus>().WithEntityAccess())
        {
            // i = level.ValueRO.current;
            // Apply each modifier according to current level
            foreach (var mod in buffer)
            {
                switch (mod.Type)
                {
                    case UpgradeModifier.IncreaseHealth:
                        if (state.EntityManager.HasComponent<MaxHealth>(entity))
                        {
                            var maxHealth = state.EntityManager.GetComponentData<MaxHealth>(entity);
                            maxHealth.value += (int)mod.Value;
                            ECB.SetComponent(entity, maxHealth);
                        }
                        if (state.EntityManager.HasComponent<Enemy>(entity))
                        {
                            var maxHealth = state.EntityManager.GetComponentData<MaxHealth>(entity);
                            var currentHealth = state.EntityManager.GetComponentData<CurrentHealth>(entity);
                            maxHealth.value += (int)mod.Value;
                            currentHealth.value = maxHealth.value;
                            ECB.SetComponent(entity, maxHealth);
                            ECB.SetComponent(entity, currentHealth);
                        }
                        break;
                    case UpgradeModifier.IncreaseDamage:
                        if (state.EntityManager.HasComponent<MeleeAttackProperties>(entity))
                        {
                            var meleeAttack = state.EntityManager.GetComponentData<MeleeAttackProperties>(entity);
                            meleeAttack.damage = (int)(meleeAttack.damage + ((mod.Value)));
                            ECB.SetComponent(entity, meleeAttack);
                        }
                        if (state.EntityManager.HasComponent<ShootAttackProperties>(entity))
                        {
                            var shootAttack = state.EntityManager.GetComponentData<ShootAttackProperties>(entity);
                            shootAttack.damage = (int)(shootAttack.damage + ((mod.Value)));
                            ECB.SetComponent(entity, shootAttack);
                        }
                        break;
                    case UpgradeModifier.DecreaseShootTime:
                        if (state.EntityManager.HasComponent<ShootAttackProperties>(entity))
                        {
                            var shootAttack = state.EntityManager.GetComponentData<ShootAttackProperties>(entity);
                            shootAttack.cooldownTickCount = (uint)(shootAttack.cooldownTickCount / ((mod.Value) + 1));
                            ECB.SetComponent(entity, shootAttack);
                        }
                        break;
                    case UpgradeModifier.IncreaseSpeed:
                        if (state.EntityManager.HasComponent<PlayerInput>(entity))
                        {
                            var ms = state.EntityManager.GetComponentData<MoveSpeed>(entity);
                            ms.value += mod.Value;
                            ECB.SetComponent(entity, ms);
                        }
                        if (state.EntityManager.HasComponent<Movement>(entity))
                        {
                            var ms = state.EntityManager.GetComponentData<MoveSpeed>(entity);
                            ms.value += mod.Value;
                            ECB.SetComponent(entity, ms);
                        }
                        break;
                    case UpgradeModifier.IncreseRange:
                        if (state.EntityManager.HasComponent<ShootAttackProperties>(entity))
                        {
                            var shootAttack = state.EntityManager.GetComponentData<ShootAttackProperties>(entity);
                            shootAttack.bulletLifeTime += (uint)((mod.Value));
                            shootAttack.bulletSpeed += (mod.Value / shootAttack.bulletSpeed + 1);
                            ECB.SetComponent(entity, shootAttack);
                        }
                        if (state.EntityManager.HasComponent<TargetRadius>(entity))
                        {
                            var targetRadius = state.EntityManager.GetComponentData<TargetRadius>(entity);
                            targetRadius.value += (mod.Value);
                            ECB.SetComponent(entity, targetRadius);
                        }
                        break;
                    case UpgradeModifier.IncreaseXpArea:
                        if (state.EntityManager.HasComponent<GetExperienceInArea>(entity))
                        {
                            var experienceInArea = state.EntityManager.GetComponentData<GetExperienceInArea>(entity);
                            experienceInArea.radius += ((mod.Value));
                            ECB.SetComponent(entity, experienceInArea);
                        }
                        break;
                    case UpgradeModifier.IncreaseHealthRegen:
                        if (state.EntityManager.HasComponent<HealthRegen>(entity))
                        {
                            var healthRegen = state.EntityManager.GetComponentData<HealthRegen>(entity);
                            healthRegen.amount += (int)(mod.Value);
                            ECB.SetComponent(entity, healthRegen);
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
