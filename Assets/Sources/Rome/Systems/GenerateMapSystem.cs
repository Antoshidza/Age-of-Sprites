using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Mathematics;

[BurstCompile]
public partial struct GenerateMapSystem : ISystem
{
    [BurstCompile]
    private struct GenerateMapJob : IJobParallelForBatch
    {
        public EntityCommandBuffer.ParallelWriter ECB;
        public float2x2 MapSize;
        [ReadOnly] public NativeArray<Entity> Rocks;
        [NativeDisableParallelForRestriction] public NativeArray<Random> PosRands;
        [NativeSetThreadIndex] private int _threadIndex;

        public void Execute(int startIndex, int count)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                var rand = PosRands[_threadIndex];
                var rockEntity = ECB.Instantiate(i, Rocks[rand.NextInt(0, Rocks.Length)]);
                ECB.SetComponent(i, rockEntity, LocalTransform2D.FromPosition(rand.NextFloat2(MapSize.c0, MapSize.c1)));
                PosRands[_threadIndex] = rand;
            }
        }
    }
    private struct SystemData : IComponentData
    {
        public Random Rand;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        state.RequireForUpdate<EndSimulationEntityCommandBufferSystem.Singleton>();
        _ = state.EntityManager.AddComponentData(state.SystemHandle, new SystemData { Rand = new Random((uint)/*System.DateTime.Now.Ticks*/1) });
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<MapSettings>(out var mapSettings))
            return;

        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
        var posRands = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
        for (int i = 0; i < posRands.Length; i++)
            posRands[i] = new Random(systemData.Rand.NextUInt());

        var generateMapJob = new GenerateMapJob
        {
            ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            MapSize = mapSettings.size,
            PosRands = posRands,
            Rocks = state.EntityManager.GetBuffer<PrefabLink>(mapSettings.rockCollectionLink).Reinterpret<Entity>().AsNativeArray()
        };
        state.Dependency = generateMapJob.ScheduleBatch(mapSettings.rockCount, 32, state.Dependency);
        _ = posRands.Dispose(state.Dependency);

        SystemAPI.SetComponent(state.SystemHandle, systemData);

        state.Enabled = false;
    }
}