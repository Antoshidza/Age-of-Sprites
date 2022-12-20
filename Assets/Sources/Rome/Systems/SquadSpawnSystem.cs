using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[WorldSystemFilter(WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct SquadSpawnSystem : ISystem
{
    private struct SystemData : IComponentData
    {
        public EntityArchetype squadArchetype;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var typeArray = new NativeArray<ComponentType>(4, Allocator.Temp);
        typeArray[0] = ComponentType.ReadOnly<WorldPosition2D>();
        typeArray[1] = ComponentType.ReadOnly<PrevWorldPosition2D>();
        typeArray[2] = ComponentType.ReadOnly<SoldierLink>();
        typeArray[3] = ComponentType.ReadOnly<RequireSoldier>();

        var systemData = new SystemData{ squadArchetype = state.EntityManager.CreateArchetype(typeArray) };

        _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);

        typeArray.Dispose();
    }

    public void OnDestroy(ref SystemState state)
    {
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!Input.GetKeyDown(KeyCode.S))
            return;

        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
        
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var squadSettings = SystemAPI.GetSingleton<SquadDefaultSettings>();
        var soldierCount = squadSettings.SoldierCount;

        var squadEntity = ecb.CreateEntity(systemData.squadArchetype);
        ecb.SetComponent(squadEntity, new RequireSoldier { count = soldierCount });
    }
}