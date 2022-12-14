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
        public EntityCommandBuffer.ParallelWriter ecb;
        public float2x2 mapSize;
        [ReadOnly] public NativeArray<Entity> rocks;
        [NativeDisableParallelForRestriction] public NativeArray<Random> posRands;
        [NativeSetThreadIndex]
        private int _threadIndex;

        public void Execute(int startIndex, int count)
        {
            for (int i = startIndex; i < startIndex + count; i++)
            {
                var rand = posRands[_threadIndex];
                var rockEntity = ecb.Instantiate(i, rocks[rand.NextInt(0, rocks.Length)]);
                ecb.SetComponent(i, rockEntity, new WorldPosition2D { value = rand.NextFloat2(mapSize.c0, mapSize.c1) });
                //ecb.SetComponent(i, rockEntity, new SpriteColor { color = new UnityEngine.Color(rand.NextFloat(0f, 1f), rand.NextFloat(0f, 1f), rand.NextFloat(0f, 1f)) });
                posRands[_threadIndex] = rand;
            }
        }
    }

    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<MapSettings>(out var mapSettings))
            return;

        var rand = new Random((uint)System.DateTime.Now.Ticks);
        var posRands = new NativeArray<Random>(JobsUtility.MaxJobThreadCount, Allocator.TempJob);
        for (int i = 0; i < posRands.Length; i++)
            posRands[i] = new Random(rand.NextUInt());

        var generateMapJob = new GenerateMapJob
        {
            ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            mapSize = mapSettings.size,
            posRands = posRands,
            rocks = state.EntityManager.GetBuffer<PrefabLink>(mapSettings.rockCollectionLink).Reinterpret<Entity>().AsNativeArray()
        };
        state.Dependency = generateMapJob.ScheduleBatch(mapSettings.rockCount, 32, state.Dependency);
        _ = posRands.Dispose(state.Dependency);

        state.Enabled = false;
    }
}