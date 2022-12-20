using NSprites;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[BurstCompile]
public partial struct SpawnNewSquadsSystem : ISystem
{
    private struct SystemData : IComponentData
    {
        public EntityQuery soldierRequireQuery;
        public EntityArchetype squadArchetype;
        public Random rand;
    }

    public void OnCreate(ref SystemState state)
    {
        var systemData = new SystemData
        {
            soldierRequireQuery = state.GetEntityQuery(typeof(RequireSoldier)),
            squadArchetype = state.EntityManager.CreateArchetype
            (
                typeof(SoldierLink),
                typeof(RequireSoldier),
                typeof(SquadSettings),

                typeof(WorldPosition2D),
                typeof(PrevWorldPosition2D)
            ),
            rand = new Random((uint)System.DateTime.Now.Ticks)
        };
        _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);
    }

    public void OnDestroy(ref SystemState state)
    {
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
        if (systemData.soldierRequireQuery.CalculateChunkCount() != 0
            || !SystemAPI.TryGetSingleton<MapSettings>(out var mapSettings)
            || !SystemAPI.TryGetSingleton<SquadDefaultSettings>(out var squadDefaultSettings))
            return;

        var pos = systemData.rand.NextFloat2(mapSettings.size.c0, mapSettings.size.c1);
        var resolution = systemData.rand.NextInt2(new int2(5), new int2(20));
        var soldierCount = resolution.x * resolution.y;

        var squadEntity = state.EntityManager.CreateEntity(systemData.squadArchetype);
        state.EntityManager.GetBuffer<SoldierLink>(squadEntity).EnsureCapacity(soldierCount);
        state.EntityManager.SetComponentData(squadEntity, new SquadSettings
        {
            squadResolution = resolution,
            soldierMargin = squadDefaultSettings.defaultSettings.soldierMargin
        });
        state.EntityManager.SetComponentData(squadEntity, new RequireSoldier { count = soldierCount });
        state.EntityManager.SetComponentData(squadEntity, new WorldPosition2D { value = pos });
        state.EntityManager.SetComponentData(squadEntity, new PrevWorldPosition2D { value = pos });

        SystemAPI.SetComponent(state.SystemHandle, systemData);
    }
}