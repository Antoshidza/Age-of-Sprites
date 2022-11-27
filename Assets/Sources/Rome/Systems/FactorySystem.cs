using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[BurstCompile]
public partial struct FactorySystem : ISystem
{
    private EntityCommandBufferSystem _ecbSystem;

    public void OnCreate(ref SystemState state)
    {
        _ecbSystem = state.World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = state.Time.DeltaTime;
        var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        state.Dependency = state.Entities.ForEach((int entityInQueryIndex, ref FactoryTimer timer, in FactoryData data) =>
        {
            timer.value -= deltaTime;

            if (timer.value <= 0)
            {
                timer.value += data.duration;
                var instanceEntities = new NativeArray<Entity>(data.count, Allocator.Temp);
                ecb.Instantiate(entityInQueryIndex, data.prefab, instanceEntities);
                for (int i = 0; i < instanceEntities.Length; i++)
                    ecb.SetComponent(entityInQueryIndex, instanceEntities[i], new WorldPosition2D { value = data.instantiatePos });
            }
        }).ScheduleParallel(state.Dependency);

        _ecbSystem.AddJobHandleForProducer(state.Dependency);
    }
}