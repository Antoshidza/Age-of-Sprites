using NSprites;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

[BurstCompile]
[UpdateBefore(typeof(SpriteUVAnimationSystem))]
public partial struct MovableAnimationControlSystem : ISystem
{
    [BurstCompile]
    private partial struct ChangeAnimationJob : IJobEntity
    {
        public AnimationSettings AnimationSettings;
        public double Time;

        private void Execute(AnimatorAspect animator, EnabledRefRO<MovingTag> movingTagEnabled)
        {
            animator.SetAnimation(movingTagEnabled.ValueRO ? AnimationSettings.WalkHash : AnimationSettings.IdleHash, Time);
        }
    }

    private struct SystemData : IComponentData
    {
        public EntityQuery MovableQuery;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var systemData = new SystemData();
        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<MovingTag>()
            .WithAspect<AnimatorAspect>()
            .WithOptions(EntityQueryOptions.IgnoreComponentEnabledState);
        var movableQuery = state.GetEntityQuery(queryBuilder);
        movableQuery.AddChangedVersionFilter(ComponentType.ReadOnly<MovingTag>());
        systemData.MovableQuery = movableQuery;

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

        var animationSwitchJob = new ChangeAnimationJob
        {
            AnimationSettings = animationSettings,
            Time = time
        };
        state.Dependency = animationSwitchJob.ScheduleParallelByRef(systemData.MovableQuery, state.Dependency);
    }
}