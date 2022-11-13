using NSprites;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
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
            ComponentType.ReadOnly<SoldierLink>()
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
        var squadSettings = state.GetSingleton<SquadSettings>();
        var soldierCount = squadSettings.squadResolution.x * squadSettings.squadResolution.y;

        var soldiersEntities = new NativeArray<Entity>(soldierCount, Allocator.Temp);
        ecb.Instantiate(squadSettings.soldierPrefab, soldiersEntities);
        var squadEntity = ecb.CreateEntity(_squadArchetype);
        var soldiersBuffer = ecb.SetBuffer<SoldierLink>(squadEntity);
        soldiersBuffer.Length = soldierCount;
        var perSoldierOffset = (2 * squadSettings.soldierMargin + 1f) * squadSettings.soldierSize;

        for (int soldierIndex = 0; soldierIndex < soldierCount; soldierIndex++)
        {
            var soldierEntity = soldiersEntities[soldierIndex];
            soldiersBuffer[soldierIndex] = new SoldierLink { entity = soldierEntity };
            ecb.SetComponent(soldierEntity, new Destination { value = perSoldierOffset * new int2(soldierIndex % squadSettings.squadResolution.x, soldierIndex / squadSettings.squadResolution.x) });
            //ecb.SetComponent(soldierEntity, new AnimationTimer { value = UnityEngine.Random.Range(0f, SpriteUVAnimationSystem.frameDuration) });
            ecb.SetComponent(soldierEntity, new MoveSpeed { value = UnityEngine.Random.Range(1f, 2f) });
        }

        _ecbSystem.AddJobHandleForProducer(state.Dependency);
    }
}