using NSprites;
using Unity.Entities;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Default)]
[UpdateInGroup(typeof(SimulationSystemGroup))]
public partial struct SquadSpawnSystem : ISystem
{
    private EntityCommandBufferSystem _ecbSystem;
    private EntityArchetype _squadArchetype;

    public void OnCreate(ref SystemState state)
    {
        _ecbSystem = state.World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
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
        
        var ecb = _ecbSystem.CreateCommandBuffer();
        var squadSettings = state.GetSingleton<SquadDefaultSettings>();
        var soldierCount = squadSettings.SoldierCount;

        var squadEntity = ecb.CreateEntity(_squadArchetype);
        ecb.SetComponent(squadEntity, new RequireSoldier { count = soldierCount });

        _ecbSystem.AddJobHandleForProducer(state.Dependency);
    }
}