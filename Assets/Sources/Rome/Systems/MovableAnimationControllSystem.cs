using NSprites;
using Unity.Burst;
using Unity.Entities;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[BurstCompile]
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
    private struct SystemData : IComponentData
    {
        public EntityQuery gotUnderWayQuery;
        public EntityQuery stopedQuery;
    }

    public void OnCreate(ref SystemState state)
    {
        var systemData = new SystemData();
        var gotUnderWayQuery = state.GetEntityQuery
        (
            ComponentType.Exclude<CullSpriteTag>(),

            ComponentType.ReadWrite<AnimationIndex>(),
            ComponentType.ReadWrite<AnimationTimer>(),
            ComponentType.ReadWrite<FrameIndex>(),
            ComponentType.ReadOnly<AnimationSetLink>(),

            ComponentType.ReadOnly<Destination>(),
            ComponentType.ReadOnly<MoveTimer>()
        );
        gotUnderWayQuery.AddOrderVersionFilter();
        systemData.gotUnderWayQuery = gotUnderWayQuery;
        var stopedQuery = state.GetEntityQuery
        (
            ComponentType.Exclude<CullSpriteTag>(),

            ComponentType.ReadWrite<AnimationIndex>(),
            ComponentType.ReadWrite<AnimationTimer>(),
            ComponentType.ReadWrite<FrameIndex>(),
            ComponentType.ReadOnly<AnimationSetLink>(),

            ComponentType.ReadOnly<Destination>(),
            ComponentType.Exclude<MoveTimer>()
        );
        stopedQuery.AddOrderVersionFilter();
        systemData.stopedQuery = stopedQuery;
        _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
        var time = SystemAPI.Time.ElapsedTime;

        var gotUnderWayChangeAnimationJob = new ChangeAnimation
        {
            setToAnimationID = CharacterAnimations.Walk,
            time = time
        };
        state.Dependency = gotUnderWayChangeAnimationJob.ScheduleParallelByRef(systemData.gotUnderWayQuery, state.Dependency);

        var stopedChangeAnimationJob = new ChangeAnimation
        {
            setToAnimationID = CharacterAnimations.Idle,
            time = time
        };
        state.Dependency = stopedChangeAnimationJob.ScheduleParallel(systemData.stopedQuery, state.Dependency);
    }
}