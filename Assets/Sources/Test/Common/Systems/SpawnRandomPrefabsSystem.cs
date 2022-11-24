using NSprites;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SpawnRandomPrefabsSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var rand = new Random((uint)System.DateTime.Now.Ticks);
        var ecb = new EntityCommandBuffer(Allocator.TempJob);
        var deltaTime = state.Time.DeltaTime;
        var time = state.Time.ElapsedTime;
        var scale2D_CDFE = state.GetComponentDataFromEntity<Scale2D>(true);
        var animDataLink_CDFE = state.GetComponentDataFromEntity<AnimationDataLink>(true);

        state.Dependency = state.Entities
            .WithReadOnly(scale2D_CDFE)
            .WithReadOnly(animDataLink_CDFE)
            .ForEach((ref FactoryTimer timer, ref SpawnerData data, in DynamicBuffer<PrefabLink> prefabs) =>
            {
                timer.value -= deltaTime;
                if (timer.value > 0f)
                    return;

                if (data.totalCount <= data.count)
                    return;

                for (int i = 0; i < data.countPerSpawn; i++)
                {
                    var entityPrefab = prefabs[rand.NextInt(0, prefabs.Length)].link;
                    var newEntity = ecb.Instantiate(entityPrefab);
                    ecb.SetComponent(newEntity, new WorldPosition2D { value = rand.NextFloat2(data.spawnBounds.c0, data.spawnBounds.c1) });
                    ref var animData = ref animDataLink_CDFE[entityPrefab].value.Value;
                    var frameIndex = rand.NextInt(0, animData.FrameDurations.Length);
                    ecb.SetComponent(newEntity, new AnimationTimer { value = time + animData.FrameDurations[frameIndex] });
                    ecb.SetComponent(newEntity, new FrameIndex { value = frameIndex });
                    ecb.SetComponent(newEntity, new Scale2D { value = scale2D_CDFE[entityPrefab].value * rand.NextFloat(0.25f, 1.25f) });
                    ecb.AddComponent(newEntity, new RandomColor { rand = new Random(rand.NextUInt()) });
                }

                data.count += data.countPerSpawn;
                data.countPerSpawn += data.spawnAcceleration;
            }).Schedule(state.Dependency);

        state.Dependency.Complete();
        ecb.Playback(state.EntityManager);
        ecb.Dispose();
    }
}