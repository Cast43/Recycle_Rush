using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Physics;
using Unity.Physics.Systems;
using UnityEngine;

[UpdateInGroup(typeof(FixedStepSimulationSystemGroup))]
[UpdateAfter(typeof(PhysicsSystemGroup))]
[UpdateAfter(typeof(PhysicsSimulationGroup))]
// [BurstCompile]
public partial struct TrashDepositPhysicsSystem : ISystem
{
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<SimulationSingleton>();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        // Pega o Singleton do ECB e cria o nosso Command Buffer
        var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
        var ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged);
        SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

        // Para acessar componentes dentro de um Job de física, usamos ComponentLookup
        var job = new DepositTriggerJob
        {
            InventoryLookup = SystemAPI.GetComponentLookup<GarbageInventory>(false),
            BinLookup = SystemAPI.GetComponentLookup<RecyclingBinData>(true),
            ECB = ecb
        };

        state.Dependency = job.Schedule(simulationSingleton, state.Dependency);
    }

    [BurstCompile]
    struct DepositTriggerJob : ITriggerEventsJob
    {
        public ComponentLookup<GarbageInventory> InventoryLookup;
        [ReadOnly] public ComponentLookup<RecyclingBinData> BinLookup;

        // A declaração do ECB aqui dentro para podermos usá-lo
        public EntityCommandBuffer ECB;

        public void Execute(TriggerEvent triggerEvent)
        {
            Entity entityA = triggerEvent.EntityA;
            Entity entityB = triggerEvent.EntityB;

            // Como não sabemos qual entidade é o robô e qual é a lixeira, verificamos ambas
            bool isRobotA = InventoryLookup.HasComponent(entityA);
            bool isRobotB = InventoryLookup.HasComponent(entityB);

            bool isBinA = BinLookup.HasComponent(entityA);
            bool isBinB = BinLookup.HasComponent(entityB);

            // Verifica se a colisão foi entre um Robô e uma Lixeira
            if (isRobotA && isBinB)
            {
                ProcessDeposit(entityA, entityB); // Robô é A, Lixeira é B
            }
            else if (isRobotB && isBinA)
            {
                ProcessDeposit(entityB, entityA); // Robô é B, Lixeira é A
            }
        }

        // Método auxiliar para processar a transferência do lixo
        private void ProcessDeposit(Entity robotEntity, Entity binEntity)
        {
            // Extrai as informações atuais
            var inventory = InventoryLookup[robotEntity];
            var binData = BinLookup[binEntity];

            int amountDeposited = 0;

            // Verifica o tipo de lixo aceito e esvazia o contador do jogador específico
            switch (binData.AcceptedType)
            {
                case TrashType.Plastic:
                    amountDeposited = inventory.PlasticCount;
                    inventory.PlasticCount = 0;
                    break;
                case TrashType.Paper:
                    amountDeposited = inventory.PaperCount;
                    inventory.PaperCount = 0;
                    break;
                case TrashType.Glass:
                    amountDeposited = inventory.GlassCount;
                    inventory.GlassCount = 0;
                    break;
                case TrashType.Iron:
                    amountDeposited = inventory.MetalCount;
                    inventory.MetalCount = 0;
                    break;
                case TrashType.Organic:
                    amountDeposited = inventory.OrganicCount;
                    inventory.OrganicCount = 0;
                    break;
                case TrashType.NotRecycle:
                    amountDeposited = inventory.NotRecycleCount;
                    inventory.NotRecycleCount = 0;
                    break;
            }

            if (amountDeposited > 0)
            {
                // Devolve os dados modificados para o inventário do robô
                InventoryLookup[robotEntity] = inventory;
                ECB.AppendToBuffer(robotEntity, new ExperienceBufferElement { value = amountDeposited });
            }
        }
    }
}