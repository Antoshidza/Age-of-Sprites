using NSprites;
using Unity.Burst;
using Unity.Entities;
using UnityEngine;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[WorldSystemFilter(WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
[BurstCompile]
public partial struct SquadSpawnSystem : ISystem
{
    private EntityArchetype _squadArchetype;

    public void OnCreate(ref SystemState state)
    {
        _squadArchetype = state.EntityManager.CreateArchetype
        (
            ComponentType.ReadOnly<WorldPosition2D>(),
            ComponentType.ReadOnly<PrevWorldPosition2D>(),
            ComponentType.ReadOnly<SoldierLink>(),
            ComponentType.ReadOnly<RequireSoldier>()
        );
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!Input.GetKeyDown(KeyCode.S))
            return;
        
        var ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);
        var squadSettings = SystemAPI.GetSingleton<SquadDefaultSettings>();
        var soldierCount = squadSettings.SoldierCount;

        var squadEntity = ecb.CreateEntity(_squadArchetype);
        ecb.SetComponent(squadEntity, new RequireSoldier { count = soldierCount });
    }
}