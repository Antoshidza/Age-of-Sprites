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
        public int SetToAnimationID;
        public double Time;

        private void Execute(ref AnimatorAspect animator)
        {
            animator.SetAnimation(SetToAnimationID, Time);
        }
    }

    private struct SystemData : IComponentData
    {
        public EntityQuery StartedToMoveQuery;
        public EntityQuery StoppedQuery;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var systemData = new SystemData();
        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithNone<CullSpriteTag>()
            .WithAspect<AnimatorAspect>()
            .WithAll<Destination, MoveTimer>();
        var startedToMoveQuery = state.GetEntityQuery(queryBuilder);
        startedToMoveQuery.AddOrderVersionFilter();
        systemData.StartedToMoveQuery = startedToMoveQuery;

        queryBuilder.Reset();
        _ = queryBuilder
            .WithNone<CullSpriteTag>()
            .WithAspect<AnimatorAspect>()
            .WithAll<Destination>()
            .WithNone<MoveTimer>();
        var stoppedQuery = state.GetEntityQuery(queryBuilder);
        stoppedQuery.AddOrderVersionFilter();
        systemData.StoppedQuery = stoppedQuery;

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
            SetToAnimationID = animationSettings.WalkHash,
            Time = time
        };
        state.Dependency = startedToMoveChangeAnimationJob.ScheduleParallelByRef(systemData.StartedToMoveQuery, state.Dependency);

        var stoppedChangeAnimationJob = new ChangeAnimationJob
        {
            SetToAnimationID = animationSettings.IdleHash,
            Time = time
        };
        state.Dependency = stoppedChangeAnimationJob.ScheduleParallel(systemData.StoppedQuery, state.Dependency);
    }
}