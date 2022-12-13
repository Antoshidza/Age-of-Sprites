using NSprites;
using Unity.Burst;
using Unity.Entities;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

public partial struct MovableAnimationControllSystem : ISystem
{
    [BurstCompile]
    private partial struct ChangeAnimation : IJobEntity
    {
        public int setToAnimationID;
        public double time;

        public void Execute(ref AnimationIndex animationIndex, ref AnimationTimer timer, ref FrameIndex frameIndex, in AnimationSetLink animationSetLink)
        {
            // find animation by animation ID
            ref var animSet = ref animationSetLink.value.Value;
            var setToAnimIndex = 0;
            for (int i = 1; i < animSet.Length; i++)
                if (animSet[i].ID == setToAnimationID)
                {
                    setToAnimIndex = i;
                    break;
                }

            if (animationIndex.value != setToAnimIndex)
            {
                animationIndex.value = setToAnimIndex;
                // here we want to set last frame and timer to 0 (equal to current time) to force animation system instantly switch
                // animation to 1st frame after we've modified it
                frameIndex.value = animSet[setToAnimIndex].FrameDurations.Length;
                timer.value = time;
            }
        }
    }

    private EntityQuery _gotUnderWayQuery;
    private EntityQuery _stopedQuery;

    public void OnCreate(ref SystemState state)
    {
        _gotUnderWayQuery = state.GetEntityQuery
        (
            ComponentType.Exclude<CullSpriteTag>(),

            ComponentType.ReadWrite<AnimationIndex>(),
            ComponentType.ReadWrite<AnimationTimer>(),
            ComponentType.ReadWrite<FrameIndex>(),
            ComponentType.ReadOnly<AnimationSetLink>(),

            ComponentType.ReadOnly<Destination>(),
            ComponentType.ReadOnly<MoveTimer>()
        );
        _gotUnderWayQuery.AddOrderVersionFilter();
        _stopedQuery = state.GetEntityQuery
        (
            ComponentType.Exclude<CullSpriteTag>(),

            ComponentType.ReadWrite<AnimationIndex>(),
            ComponentType.ReadWrite<AnimationTimer>(),
            ComponentType.ReadWrite<FrameIndex>(),
            ComponentType.ReadOnly<AnimationSetLink>(),

            ComponentType.ReadOnly<Destination>(),
            ComponentType.Exclude<MoveTimer>()
        );
        _stopedQuery.AddOrderVersionFilter();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var time = SystemAPI.Time.ElapsedTime;

        var gotUnderWayChangeAnimationJob = new ChangeAnimation
        {
            setToAnimationID = CharacterAnimations.Walk,
            time = time
        };
        state.Dependency = gotUnderWayChangeAnimationJob.ScheduleParallelByRef(_gotUnderWayQuery, state.Dependency);

        var stopedChangeAnimationJob = new ChangeAnimation
        {
            setToAnimationID = CharacterAnimations.Idle,
            time = time
        };
        state.Dependency = stopedChangeAnimationJob.ScheduleParallel(_stopedQuery, state.Dependency);
    }
}