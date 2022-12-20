using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[BurstCompile]
public partial struct FactorySystem : ISystem
{
    [BurstCompile]
    private partial struct ProductionJob : IJobEntity
    {
        public float deltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;

        private void Execute([ChunkIndexInQuery] int chunkIndex, ref FactoryTimer timer, in FactoryData data)
        {
            timer.value -= deltaTime;

            if (timer.value <= 0)
            {
                timer.value += data.duration;
                var instanceEntities = new NativeArray<Entity>(data.count, Allocator.Temp);
                ecb.Instantiate(chunkIndex, data.prefab, instanceEntities);
                for (int i = 0; i < instanceEntities.Length; i++)
                    ecb.SetComponent(chunkIndex, instanceEntities[i], new WorldPosition2D { value = data.instantiatePos });
            }
        }
    }

    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var productionJob = new ProductionJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            ecb = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        };
        state.Dependency = productionJob.ScheduleParallelByRef(state.Dependency);
    }
}