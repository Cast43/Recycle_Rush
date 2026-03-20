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
    public void OnCreate(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        EntityCommandBuffer ECB = new EntityCommandBuffer(Allocator.Temp);

        foreach (var (buffer, entity) in SystemAPI.Query<DynamicBuffer<StatusModifier>>().WithAll<UpdateStatus>().WithEntityAccess())
        {
            // 1. Lemos a existência (HasComponent) e os valores (GetComponent) UMA VEZ antes do loop.
            bool hasMaxHealth = SystemAPI.HasComponent<MaxHealth>(entity);
            var maxHealth = hasMaxHealth ? SystemAPI.GetComponent<MaxHealth>(entity) : default;

            bool hasCurrentHealth = SystemAPI.HasComponent<CurrentHealth>(entity);
            var currentHealth = hasCurrentHealth ? SystemAPI.GetComponent<CurrentHealth>(entity) : default;
            bool hasEnemy = SystemAPI.HasComponent<Enemy>(entity);

            bool hasMelee = SystemAPI.HasComponent<MeleeAttackProperties>(entity);
            var meleeAttack = hasMelee ? SystemAPI.GetComponent<MeleeAttackProperties>(entity) : default;

            bool hasShoot = SystemAPI.HasComponent<ShootAttackProperties>(entity);
            var shootAttack = hasShoot ? SystemAPI.GetComponent<ShootAttackProperties>(entity) : default;

            bool hasMoveSpeed = SystemAPI.HasComponent<MoveSpeed>(entity);
            var moveSpeed = hasMoveSpeed ? SystemAPI.GetComponent<MoveSpeed>(entity) : default;
            bool hasPlayerInput = SystemAPI.HasComponent<PlayerInput>(entity);
            bool hasMovement = SystemAPI.HasComponent<Movement>(entity);

            bool hasTargetRadius = SystemAPI.HasComponent<TargetRadius>(entity);
            var targetRadius = hasTargetRadius ? SystemAPI.GetComponent<TargetRadius>(entity) : default;

            bool hasXpArea = SystemAPI.HasComponent<GetExperienceInArea>(entity);
            var experienceInArea = hasXpArea ? SystemAPI.GetComponent<GetExperienceInArea>(entity) : default;

            bool hasHealthRegen = SystemAPI.HasComponent<HealthRegen>(entity);
            var healthRegen = hasHealthRegen ? SystemAPI.GetComponent<HealthRegen>(entity) : default;

            // Flags para sabermos se precisamos salvar o componente no ECB no final
            bool saveHealth = false, saveMelee = false, saveShoot = false;
            bool saveMoveSpeed = false, saveTargetRadius = false;
            bool saveXpArea = false, saveHealthRegen = false;

            // 2. Aplicamos TODOS os modificadores nas variáveis locais em memória
            foreach (var mod in buffer)
            {
                switch (mod.Type)
                {
                    case UpgradeModifier.IncreaseHealth:
                        if (hasMaxHealth)
                        {
                            maxHealth.value += (int)mod.Value;
                            saveHealth = true;

                            if (hasEnemy && hasCurrentHealth)
                            {
                                currentHealth.value = maxHealth.value;
                            }
                        }
                        break;

                    case UpgradeModifier.IncreaseDamage:
                        if (hasMelee)
                        {
                            meleeAttack.damage += (int)mod.Value;
                            saveMelee = true;
                        }
                        if (hasShoot)
                        {
                            shootAttack.damage += (int)mod.Value;
                            saveShoot = true;
                        }
                        break;

                    case UpgradeModifier.DecreaseShootTime:
                        if (hasShoot)
                        {
                            shootAttack.cooldownTickCount = (uint)(shootAttack.cooldownTickCount / (mod.Value + 1));
                            saveShoot = true;
                        }
                        break;

                    case UpgradeModifier.IncreaseSpeed:
                        // No seu código original, se a entidade tivesse PlayerInput E Movement, 
                        // a velocidade era somada duas vezes. Assumi que isso era um bug e unifiquei a lógica.
                        if (hasMoveSpeed && (hasPlayerInput || hasMovement))
                        {
                            moveSpeed.maxSpeed += mod.Value;
                            saveMoveSpeed = true;
                        }
                        break;

                    case UpgradeModifier.IncreseRange:
                        if (hasShoot)
                        {
                            shootAttack.bulletLifeTime += (uint)mod.Value;
                            shootAttack.bulletSpeed += (mod.Value / shootAttack.bulletSpeed + 1);
                            saveShoot = true;
                        }
                        if (hasTargetRadius)
                        {
                            targetRadius.value += mod.Value;
                            saveTargetRadius = true;
                        }
                        break;

                    case UpgradeModifier.IncreaseXpArea:
                        if (hasXpArea)
                        {
                            experienceInArea.radius += mod.Value;
                            saveXpArea = true;
                        }
                        break;

                    case UpgradeModifier.IncreaseHealthRegen:
                        if (hasHealthRegen)
                        {
                            healthRegen.amount += (int)mod.Value;
                            saveHealthRegen = true;
                        }
                        break;
                }

                // Debug log pode ficar muito pesado se houver muitos mods, mas mantive como estava.
                Debug.Log(mod.Type);
            }

            // 3. Gravamos as alterações UMA ÚNICA VEZ no ECB por entidade
            if (saveHealth)
            {
                ECB.SetComponent(entity, maxHealth);
                if (hasEnemy && hasCurrentHealth) ECB.SetComponent(entity, currentHealth);
            }
            if (saveMelee) ECB.SetComponent(entity, meleeAttack);
            if (saveShoot) ECB.SetComponent(entity, shootAttack);
            if (saveMoveSpeed) ECB.SetComponent(entity, moveSpeed);
            if (saveTargetRadius) ECB.SetComponent(entity, targetRadius);
            if (saveXpArea) ECB.SetComponent(entity, experienceInArea);
            if (saveHealthRegen) ECB.SetComponent(entity, healthRegen);

            // Limpeza
            buffer.Clear();
            ECB.RemoveComponent<UpdateStatus>(entity);
        }

        ECB.Playback(state.EntityManager);
        ECB.Dispose();
    }
}