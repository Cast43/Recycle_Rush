using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;
using Unity.Collections;

[WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
[UpdateInGroup(typeof(PredictedSimulationSystemGroup), OrderLast = true)]
[UpdateAfter(typeof(CalculateFrameExperienceSystem))]
partial struct UpdateStatusSystem : ISystem
{
    private ComponentLookup<MaxHealth> maxHealthLookup;
    private ComponentLookup<CurrentHealth> currentHealthLookup;
    private ComponentLookup<Enemy> enemyLookup;
    private ComponentLookup<MeleeAttackProperties> meleeAttackLookup;
    private ComponentLookup<ShootAttackProperties> shootAttackLookup;
    private ComponentLookup<MoveSpeed> moveSpeedLookup;
    private ComponentLookup<PlayerInput> playerInputLookup;
    private ComponentLookup<Movement> movementLookup;
    private ComponentLookup<TargetRadius> targetRadiusLookup;
    private ComponentLookup<GetExperienceInArea> xpAreaLookup;
    private ComponentLookup<HealthRegen> healthRegenLookup;

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();

        maxHealthLookup = state.GetComponentLookup<MaxHealth>(false);
        currentHealthLookup = state.GetComponentLookup<CurrentHealth>(false);
        enemyLookup = state.GetComponentLookup<Enemy>(true);
        meleeAttackLookup = state.GetComponentLookup<MeleeAttackProperties>(false);
        shootAttackLookup = state.GetComponentLookup<ShootAttackProperties>(false);
        moveSpeedLookup = state.GetComponentLookup<MoveSpeed>(false);
        playerInputLookup = state.GetComponentLookup<PlayerInput>(true);
        movementLookup = state.GetComponentLookup<Movement>(true);
        targetRadiusLookup = state.GetComponentLookup<TargetRadius>(false);
        xpAreaLookup = state.GetComponentLookup<GetExperienceInArea>(false);
        healthRegenLookup = state.GetComponentLookup<HealthRegen>(false);
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        maxHealthLookup.Update(ref state);
        currentHealthLookup.Update(ref state);
        enemyLookup.Update(ref state);
        meleeAttackLookup.Update(ref state);
        shootAttackLookup.Update(ref state);
        moveSpeedLookup.Update(ref state);
        playerInputLookup.Update(ref state);
        movementLookup.Update(ref state);
        targetRadiusLookup.Update(ref state);
        xpAreaLookup.Update(ref state);
        healthRegenLookup.Update(ref state);

        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        var updateJob = new UpdateStatusJob
        {
            maxHealthLookup = maxHealthLookup,
            currentHealthLookup = currentHealthLookup,
            enemyLookup = enemyLookup,
            meleeAttackLookup = meleeAttackLookup,
            shootAttackLookup = shootAttackLookup,
            moveSpeedLookup = moveSpeedLookup,
            playerInputLookup = playerInputLookup,
            movementLookup = movementLookup,
            targetRadiusLookup = targetRadiusLookup,
            xpAreaLookup = xpAreaLookup,
            healthRegenLookup = healthRegenLookup,
            ECB = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged)
        };

        state.Dependency = updateJob.Schedule(state.Dependency);
    }
}

[BurstCompile]
[WithAll(typeof(UpdateStatus))]
public partial struct UpdateStatusJob : IJobEntity
{
    public ComponentLookup<MaxHealth> maxHealthLookup;
    public ComponentLookup<CurrentHealth> currentHealthLookup;
    [ReadOnly] public ComponentLookup<Enemy> enemyLookup;
    public ComponentLookup<MeleeAttackProperties> meleeAttackLookup;
    public ComponentLookup<ShootAttackProperties> shootAttackLookup;
    public ComponentLookup<MoveSpeed> moveSpeedLookup;
    [ReadOnly] public ComponentLookup<PlayerInput> playerInputLookup;
    [ReadOnly] public ComponentLookup<Movement> movementLookup;
    public ComponentLookup<TargetRadius> targetRadiusLookup;
    public ComponentLookup<GetExperienceInArea> xpAreaLookup;
    public ComponentLookup<HealthRegen> healthRegenLookup;

    public EntityCommandBuffer ECB;

    public void Execute(Entity entity, ref DynamicBuffer<StatusModifier> buffer)
    {
        bool hasMaxHealth = maxHealthLookup.HasComponent(entity);
        var maxHealth = hasMaxHealth ? maxHealthLookup[entity] : default;

        bool hasCurrentHealth = currentHealthLookup.HasComponent(entity);
        var currentHealth = hasCurrentHealth ? currentHealthLookup[entity] : default;
        
        bool hasEnemy = enemyLookup.HasComponent(entity);

        bool hasMelee = meleeAttackLookup.HasComponent(entity);
        var meleeAttack = hasMelee ? meleeAttackLookup[entity] : default;

        bool hasShoot = shootAttackLookup.HasComponent(entity);
        var shootAttack = hasShoot ? shootAttackLookup[entity] : default;

        bool hasMoveSpeed = moveSpeedLookup.HasComponent(entity);
        var moveSpeed = hasMoveSpeed ? moveSpeedLookup[entity] : default;
        
        bool hasPlayerInput = playerInputLookup.HasComponent(entity);
        bool hasMovement = movementLookup.HasComponent(entity);

        bool hasTargetRadius = targetRadiusLookup.HasComponent(entity);
        var targetRadius = hasTargetRadius ? targetRadiusLookup[entity] : default;

        bool hasXpArea = xpAreaLookup.HasComponent(entity);
        var experienceInArea = hasXpArea ? xpAreaLookup[entity] : default;

        bool hasHealthRegen = healthRegenLookup.HasComponent(entity);
        var healthRegen = hasHealthRegen ? healthRegenLookup[entity] : default;

        bool saveHealth = false, saveMelee = false, saveShoot = false;
        bool saveMoveSpeed = false, saveTargetRadius = false;
        bool saveXpArea = false, saveHealthRegen = false;

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
        }

        if (saveHealth)
        {
            maxHealthLookup[entity] = maxHealth;
            if (hasEnemy && hasCurrentHealth) currentHealthLookup[entity] = currentHealth;
        }
        if (saveMelee) meleeAttackLookup[entity] = meleeAttack;
        if (saveShoot) shootAttackLookup[entity] = shootAttack;
        if (saveMoveSpeed) moveSpeedLookup[entity] = moveSpeed;
        if (saveTargetRadius) targetRadiusLookup[entity] = targetRadius;
        if (saveXpArea) xpAreaLookup[entity] = experienceInArea;
        if (saveHealthRegen) healthRegenLookup[entity] = healthRegen;

        buffer.Clear();
        ECB.RemoveComponent<UpdateStatus>(entity);
    }
}