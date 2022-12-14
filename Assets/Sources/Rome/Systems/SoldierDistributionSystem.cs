using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[BurstCompile]
public partial struct SoldierDistributionSystem : ISystem
{
    private EntityQuery _soldierLessSquadQuery;
    private EntityQuery _freeSoldiersQuery;

    [BurstCompile]
    private struct DistributeJob : IJob
    {
        [ReadOnly] public NativeList<Entity> squadEntities;
        [ReadOnly] public NativeList<RequireSoldier> requireSoldierData;
        [ReadOnly] public NativeList<Entity> soldierEntities;
        public EntityCommandBuffer ecb;
        public BufferLookup<SoldierLink> soldierLink_BFE;
        [WriteOnly] public ComponentLookup<RequireSoldier> requireSoldier_CDFE_WO;

        public void Execute()
        {
            if (soldierEntities.Length == 0 || squadEntities.Length == 0)
                return;

            var soldierIndex = 0;
            var prevSquadIndex = -1;
            var squadIndex = 0;
            DynamicBuffer<SoldierLink> soldierLinkBuffer = default;

            while (soldierIndex < soldierEntities.Length && squadIndex < squadEntities.Length)
            {
                if (squadIndex != prevSquadIndex)
                {
                    soldierLinkBuffer = soldierLink_BFE[squadEntities[squadIndex]];
                    prevSquadIndex = squadIndex;
                }
                var requireSoldier = requireSoldierData[squadIndex];
                var distributionCount = math.min(soldierEntities.Length - soldierIndex, requireSoldier.count);
                soldierLinkBuffer.Capacity += distributionCount;

                for (int i = soldierIndex; i < distributionCount; i++)
                {
                    var soldierEntity = soldierEntities[i];
                    _ = soldierLinkBuffer.Add(new SoldierLink { entity = soldierEntity });
                    ecb.AddComponent<InSquadSoldierTag>(soldierEntity);
                }

                soldierIndex += distributionCount;

                requireSoldier.count -= distributionCount;
                // means squad is full so we can just remove comp
                if (requireSoldier.count == 0)
                    ecb.RemoveComponent<RequireSoldier>(squadEntities[squadIndex++]);
                // means squad isn't full AND there is no more soldiers so we should update comp
                else if (soldierIndex >= soldierEntities.Length)
                    requireSoldier_CDFE_WO[squadEntities[squadIndex]] = requireSoldier;
            }
        }
    }

    public void OnCreate(ref SystemState state)
    {
        _soldierLessSquadQuery = state.GetEntityQuery(typeof(RequireSoldier));
        _freeSoldiersQuery = state.GetEntityQuery
        (
            ComponentType.ReadOnly<SoldierTag>(),
            ComponentType.Exclude<InSquadSoldierTag>()
        );
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var squadEntities = _soldierLessSquadQuery.ToEntityListAsync(Allocator.TempJob, out var squadEntities_GatherHandle);
        var requireSoldierData = _soldierLessSquadQuery.ToComponentDataListAsync<RequireSoldier>(Allocator.TempJob, state.Dependency, out var requireSoldier_GatherHandle);
        var soldierEntities = _freeSoldiersQuery.ToEntityListAsync(Allocator.TempJob, out var soldierEntities_GatherHandle);
        var distributeJob = new DistributeJob
        {
            squadEntities = squadEntities,
            requireSoldierData = requireSoldierData,
            soldierEntities = soldierEntities,
            ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged),
            soldierLink_BFE = SystemAPI.GetBufferLookup<SoldierLink>(false),
            requireSoldier_CDFE_WO = SystemAPI.GetComponentLookup<RequireSoldier>(false)
        };

        var inputHandles = new NativeArray<JobHandle>(4, Allocator.Temp);
        inputHandles[0] = squadEntities_GatherHandle;
        inputHandles[1] = requireSoldier_GatherHandle;
        inputHandles[2] = soldierEntities_GatherHandle;
        inputHandles[3] = state.Dependency;

        state.Dependency = distributeJob.ScheduleByRef(JobHandle.CombineDependencies(inputHandles));
        _ = squadEntities.Dispose(state.Dependency);
        _ = requireSoldierData.Dispose(state.Dependency);
        _ = soldierEntities.Dispose(state.Dependency);
    }
}