using NSprites;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct MoveToDestinationSystem : ISystem
{
    #region jobs
    [BurstCompile]
    private struct CalculateMoveTimerJob : IJobChunk
    {
        private const float Threshold = .01f;

        [ReadOnly] public EntityTypeHandle EntityTypeHandle;
        public ComponentTypeHandle<MoveTimer> MoveTimer_CTH_RW;
        [ReadOnly] public ComponentTypeHandle<MoveSpeed> MoveSpeed_CTH_RO;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld2D> LTW2D_CTH_RO;
        [ReadOnly] public ComponentTypeHandle<Destination> Destionation_CTH_RO;
        public ComponentTypeHandle<MovingTag> MovingTag_CTH_RW;
        public uint LastSystemVersion;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.DidChange(ref Destionation_CTH_RO, LastSystemVersion)
                || chunk.DidChange(ref MoveSpeed_CTH_RO, LastSystemVersion))
            {
                var entities = chunk.GetNativeArray(EntityTypeHandle);
                var ltw2D = chunk.GetNativeArray(ref LTW2D_CTH_RO);
                var moveSpeeds = chunk.GetNativeArray(ref MoveSpeed_CTH_RO);
                var destinations = chunk.GetNativeArray(ref Destionation_CTH_RO);
                var timers = chunk.GetNativeArray(ref MoveTimer_CTH_RW);

                for (int entityIndex = 0; entityIndex < entities.Length; entityIndex++)
                {
                    var distance = math.length(destinations[entityIndex].value - ltw2D[entityIndex].Position);
                    if (distance > Threshold)
                    {
                        timers[entityIndex] = new MoveTimer { RemainingTime = GetRemainingTime(distance, moveSpeeds[entityIndex].value) };
                        if (!chunk.IsComponentEnabled(ref MovingTag_CTH_RW, entityIndex))
                            chunk.SetComponentEnabled(ref MovingTag_CTH_RW, entityIndex, true);
                    }
                }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetRemainingTime(in float2 pos, in float2 dest, float speed)
            => GetRemainingTime(math.length(dest - pos), speed);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetRemainingTime(in float distance, float speed)
            => distance / speed;
    }
    [BurstCompile]
    [WithAll(typeof(MovingTag))]
    private partial struct MoveJob : IJobEntity
    {
        public float DeltaTime;
        [NativeDisableParallelForRestriction] public ComponentLookup<MovingTag> MovingTag_CL_RW;

        private void Execute(Entity entity, ref LocalTransform2D transform, ref MoveTimer timer, in Destination destination)
        {
            var remainingDelta = math.max(timer.RemainingTime, DeltaTime - timer.RemainingTime);
            // move pos in a direction of current destination by passed frac of whole remaining move time
            transform.Position += (destination.value - transform.Position) * DeltaTime / remainingDelta;
            timer.RemainingTime = math.max(0, timer.RemainingTime - DeltaTime);

            if (timer.RemainingTime == 0f)
                MovingTag_CL_RW.SetComponentEnabled(entity, false);
        }
    }
    #endregion

    private struct SystemData : IComponentData
    {
        public EntityQuery MovableQuery;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<MoveSpeed>()
            .WithAll<LocalToWorld2D>()
            .WithAll<Destination>();
        var systemData = new SystemData{ MovableQuery = state.GetEntityQuery(queryBuilder) };
        _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);

        queryBuilder.Dispose();
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);

        // recalculate MoveTimer if MoveSpeed or Destination was changed
        var calculateMoveTimerJob = new CalculateMoveTimerJob
        {
            EntityTypeHandle = SystemAPI.GetEntityTypeHandle(),
            MoveTimer_CTH_RW = SystemAPI.GetComponentTypeHandle<MoveTimer>(false),
            MoveSpeed_CTH_RO = SystemAPI.GetComponentTypeHandle<MoveSpeed>(true),
            LTW2D_CTH_RO = SystemAPI.GetComponentTypeHandle<LocalToWorld2D>(true),
            Destionation_CTH_RO = SystemAPI.GetComponentTypeHandle<Destination>(true),
            MovingTag_CTH_RW = SystemAPI.GetComponentTypeHandle<MovingTag>(false),
            LastSystemVersion = state.LastSystemVersion
        };
        state.Dependency = calculateMoveTimerJob.ScheduleParallelByRef(systemData.MovableQuery, state.Dependency);

        var moveJob = new MoveJob
        {
            DeltaTime = SystemAPI.Time.DeltaTime,
            MovingTag_CL_RW = SystemAPI.GetComponentLookup<MovingTag>(false)
        };
        state.Dependency = moveJob.ScheduleParallelByRef(state.Dependency);
    }
}