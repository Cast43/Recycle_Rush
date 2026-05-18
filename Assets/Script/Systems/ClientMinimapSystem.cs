using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.NetCode;
using Unity.Mathematics;

[WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
public partial class ClientMinimapSystem : SystemBase
{
    private EntityQuery playerQuery;
    private EntityQuery enemyQuery;
    private EntityQuery garbageQuery;
    private EntityQuery eventQuery;
    private EntityQuery binQuery;

    protected override void OnCreate()
    {
        playerQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<PlayerInput>(), ComponentType.ReadOnly<GhostOwnerIsLocal>());
        enemyQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<Enemy>());
        garbageQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<GiveExperience>());
        eventQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<EventObjective>(), ComponentType.ReadOnly<EventActiveTag>());
        binQuery = GetEntityQuery(ComponentType.ReadOnly<LocalTransform>(), ComponentType.ReadOnly<RecyclingBinData>());
    }

    protected override void OnUpdate()
    {
        // Só processa e gasta processamento se o mapa estiver visível na tela
        if (MinimapManager.Instance == null || !MinimapManager.Instance.IsMapVisible()) return;

        float3 playerPos = float3.zero;
        if (!playerQuery.IsEmptyIgnoreFilter)
        {
            using var players = playerQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
            if (players.Length > 0) playerPos = players[0].Position;
        }

        using var enemies = enemyQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using var garbages = garbageQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using var events = eventQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using var bins = binQuery.ToComponentDataArray<LocalTransform>(Allocator.Temp);
        using var binData = binQuery.ToComponentDataArray<RecyclingBinData>(Allocator.Temp);

        NativeArray<float3> enemyPositions = new NativeArray<float3>(enemies.Length, Allocator.Temp);
        NativeArray<float3> garbagePositions = new NativeArray<float3>(garbages.Length, Allocator.Temp);
        NativeArray<float3> eventPositions = new NativeArray<float3>(events.Length, Allocator.Temp);
        NativeArray<float3> binPositions = new NativeArray<float3>(bins.Length, Allocator.Temp);
        NativeArray<TrashType> binTypes = new NativeArray<TrashType>(bins.Length, Allocator.Temp);

        for (int i = 0; i < enemies.Length; i++) enemyPositions[i] = enemies[i].Position;
        for (int i = 0; i < garbages.Length; i++) garbagePositions[i] = garbages[i].Position;
        for (int i = 0; i < events.Length; i++) eventPositions[i] = events[i].Position;
        for (int i = 0; i < bins.Length; i++) 
        {
            binPositions[i] = bins[i].Position;
            binTypes[i] = binData[i].AcceptedType;
        }

        // Envia as informações para o Manager de UI
        MinimapManager.Instance.UpdateMap(playerPos, enemyPositions, garbagePositions, eventPositions, binPositions, binTypes);

        // Libera a memória temporária
        enemyPositions.Dispose();
        garbagePositions.Dispose();
        eventPositions.Dispose();
        binPositions.Dispose();
        binTypes.Dispose();
    }
}