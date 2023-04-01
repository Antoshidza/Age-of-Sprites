using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateBefore(typeof(SpriteUVAnimationSystem))]
public partial struct MovableAnimationControlSystem : ISystem
{
    [BurstCompile(FloatPrecision.High, FloatMode.Default)]
    private partial struct ChangeAnimationJob : IJobEntity
    {
        public int setToAnimationID;
        public double time;

        public void Execute(Entity entity, ref AnimationIndex animationIndex, ref AnimationTimer timer, ref FrameIndex frameIndex, in AnimationSetLink animationSetLink)
        {
            // find animation by animation ID
            ref var animSet = ref animationSetLink.value.Value;
            var setToAnimIndex = -1;
            for (int i = 0; i < animSet.Length; i++)
                if (animSet[i].ID == setToAnimationID)
                {
                    setToAnimIndex = i;
                    break;
                }

            if (setToAnimIndex == -1)
                throw new NSpritesException($"{nameof(ChangeAnimationJob)}: incorrect {nameof(setToAnimationID)} was passed. {entity} has no animation with such hash ({setToAnimationID}) was found");

            if (animationIndex.value != setToAnimIndex)
            {
                // Debug.Log($"{setToAnimationID} was founded under {setToAnimIndex} index");
                ref var animData = ref animSet[setToAnimIndex];
                animationIndex.value = setToAnimIndex;
                // here we want to set last frame and timer to 0 (equal to current time) to force animation system instantly switch
                // animation to 1st frame after we've modified it
                frameIndex.value = animData.FrameDurations.Length - 1;
                timer.value = time;
            }
        }
    }

    private struct SystemData : IComponentData
    {
        public EntityQuery startedToMoveQuery;
        public EntityQuery stoppedQuery;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var systemData = new SystemData();
        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithNone<CullSpriteTag>()
            .WithAllRW<AnimationIndex>()
            .WithAllRW<AnimationTimer>()
            .WithAllRW<FrameIndex>()
            .WithAll<AnimationSetLink>()
            .WithAll<Destination, MoveTimer>();
        var startedToMoveQuery = state.GetEntityQuery(queryBuilder);
        startedToMoveQuery.AddOrderVersionFilter();
        systemData.startedToMoveQuery = startedToMoveQuery;

        queryBuilder.Reset();
        _ = queryBuilder
            .WithNone<CullSpriteTag>()
            .WithAllRW<AnimationIndex>()
            .WithAllRW<AnimationTimer>()
            .WithAllRW<FrameIndex>()
            .WithAll<AnimationSetLink>()
            .WithAll<Destination>()
            .WithNone<MoveTimer>();
        var stoppedQuery = state.GetEntityQuery(queryBuilder);
        stoppedQuery.AddOrderVersionFilter();
        systemData.stoppedQuery = stoppedQuery;

        _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);

        queryBuilder.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
        if (!SystemAPI.TryGetSingleton<AnimationSettings>(out var animationSettings))
            return;
        var time = SystemAPI.Time.ElapsedTime;

        var startedToMoveChangeAnimationJob = new ChangeAnimationJob
        {
            setToAnimationID = animationSettings.WalkHash,
            time = time
        };
        state.Dependency = startedToMoveChangeAnimationJob.ScheduleParallelByRef(systemData.startedToMoveQuery, state.Dependency);

        var stoppedChangeAnimationJob = new ChangeAnimationJob
        {
            setToAnimationID = animationSettings.IdleHash,
            time = time
        };
        state.Dependency = stoppedChangeAnimationJob.ScheduleParallel(systemData.stoppedQuery, state.Dependency);
    }
}