using NSprites;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct SpawnNewSquadsSystem : ISystem
{
    private struct SystemData : IComponentData
    {
        public EntityQuery SoldierRequireQuery;
        public EntityArchetype SquadArchetype;
        public Random Rand;
    }

    public void OnCreate(ref SystemState state)
    {
        var systemData = new SystemData
        {
            SoldierRequireQuery = state.GetEntityQuery(typeof(RequireSoldier)),
            SquadArchetype = state.EntityManager.CreateArchetype
            (
                typeof(SoldierLink),
                typeof(RequireSoldier),
                typeof(SquadSettings),

                typeof(LocalTransform2D),
                typeof(LocalToWorld2D),
                typeof(PrevWorldPosition2D)
            ),
            Rand = new Random((uint)1/*System.DateTime.Now.Ticks*/)
        };
        _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
        if (systemData.SoldierRequireQuery.CalculateChunkCount() != 0
            || !SystemAPI.TryGetSingleton<MapSettings>(out var mapSettings)
            || !SystemAPI.TryGetSingleton<SquadDefaultSettings>(out var squadDefaultSettings))
            return;

        var pos = systemData.Rand.NextFloat2(mapSettings.size.c0, mapSettings.size.c1);
        var resolution = systemData.Rand.NextInt2(new int2(5), new int2(20));
        var soldierCount = resolution.x * resolution.y;

        var squadEntity = state.EntityManager.CreateEntity(systemData.SquadArchetype);
        state.EntityManager.GetBuffer<SoldierLink>(squadEntity).EnsureCapacity(soldierCount);
        state.EntityManager.SetComponentData(squadEntity, new SquadSettings
        {
            squadResolution = resolution,
            soldierMargin = squadDefaultSettings.defaultSettings.soldierMargin
        });
        state.EntityManager.SetComponentData(squadEntity, new RequireSoldier { count = soldierCount });
        state.EntityManager.SetComponentData(squadEntity, LocalTransform2D.FromPosition(pos));
        state.EntityManager.SetComponentData(squadEntity, new PrevWorldPosition2D { value = pos });

        SystemAPI.SetComponent(state.SystemHandle, systemData);
    }
}