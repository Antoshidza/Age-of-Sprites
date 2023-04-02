using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
public partial struct FactorySystem : ISystem
{
    [BurstCompile]
    private partial struct ProductionJob : IJobEntity
    {
        public float DeltaTime;
        public EntityCommandBuffer.ParallelWriter ECB;

        private void Execute([ChunkIndexInQuery] int chunkIndex, ref FactoryTimer timer, in FactoryData data)
        {
            timer.value -= DeltaTime;

            if (timer.value <= 0)
            {
                timer.value += data.duration;
                var instanceEntities = new NativeArray<Entity>(data.count, Allocator.Temp);
                ECB.Instantiate(chunkIndex, data.prefab, instanceEntities);
                for (int i = 0; i < instanceEntities.Length; i++)
                    ECB.SetComponent(chunkIndex, instanceEntities[i], new WorldPosition2D { value = data.instantiatePos });
            }
        }
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var productionJob = new ProductionJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            ECB = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        };
        state.Dependency = productionJob.ScheduleParallelByRef(state.Dependency);
    }
}