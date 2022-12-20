using NSprites;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[BurstCompile]
public partial struct MoveToDestinationSystem : ISystem
{
    #region jobs
    [BurstCompile]
    private struct CalculateMoveTimerJob : IJobChunk
    {
        private const float threshold = .01f;

        [ReadOnly] public EntityTypeHandle entityTypeHandle;
        public ComponentTypeHandle<MoveTimer> moveTimer_CTH_RW;
        [ReadOnly] public ComponentTypeHandle<MoveSpeed> moveSpeed_CTH_RO;
        [ReadOnly] public ComponentTypeHandle<WorldPosition2D> worldPosition2D_CTH_RO;
        [ReadOnly] public ComponentTypeHandle<Destination> destionation_CTH_RO;
        public EntityCommandBuffer.ParallelWriter ecb;
        public uint lastSystemVersion;

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            if (chunk.DidChange(ref destionation_CTH_RO, lastSystemVersion)
                || chunk.DidChange(ref moveSpeed_CTH_RO, lastSystemVersion))
            {
                var entities = chunk.GetNativeArray(entityTypeHandle);
                var worldPositions = chunk.GetNativeArray(ref worldPosition2D_CTH_RO);
                var moveSpeeds = chunk.GetNativeArray(ref moveSpeed_CTH_RO);
                var destionations = chunk.GetNativeArray(ref destionation_CTH_RO);
                var timers = chunk.GetNativeArray(ref moveTimer_CTH_RW);

                if (chunk.Has(ref moveTimer_CTH_RW))
                    for (int entityIndex = 0; entityIndex < worldPositions.Length; entityIndex++)
                        timers[entityIndex] = new MoveTimer { remainingTime = GetRamainingTime(worldPositions[entityIndex].value, destionations[entityIndex].value, moveSpeeds[entityIndex].value) };
                else
                    for (int entityIndex = 0; entityIndex < worldPositions.Length; entityIndex++)
                    {
                        var distance = math.length(destionations[entityIndex].value - worldPositions[entityIndex].value);
                        if (distance > threshold)
                            ecb.AddComponent(unfilteredChunkIndex, entities[entityIndex], new MoveTimer { remainingTime = GetRamainingTime(distance, moveSpeeds[entityIndex].value) });
                    }
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetRamainingTime(in float2 pos, in float2 dest, float speed)
            => GetRamainingTime(math.length(dest - pos), speed);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static float GetRamainingTime(in float distance, float speed)
            => distance / speed;
    }
    [BurstCompile]
    private partial struct MoveJob : IJobEntity
    {
        public float deltaTime;
        public EntityCommandBuffer.ParallelWriter ecb;

        private void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, ref WorldPosition2D pos, ref MoveTimer timer, in Destination destination)
        {
            var remainingDelta = math.max(timer.remainingTime, deltaTime - timer.remainingTime);
            // move pos in a direction of current destination by passed frac of whole remaining move time
            pos.value += (destination.value - pos.value) * deltaTime / remainingDelta;
            timer.remainingTime = math.max(0, timer.remainingTime - deltaTime);

            if (timer.remainingTime == 0f)
                ecb.RemoveComponent<MoveTimer>(chunkIndex, entity);
        }
    }
    #endregion

    private struct SystemData : IComponentData
    {
        public EntityQuery movableQuery;
    }

    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<MoveSpeed>()
            .WithAll<WorldPosition2D>()
            .WithAll<Destination>();
        var systemData = new SystemData{ movableQuery = state.GetEntityQuery(queryBuilder) };
        _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);

        queryBuilder.Dispose();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
        var ecbSystem = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

        /// [re]calculate <see cref="MoveTimer"/> if <see cref="MoveSpeed"/> or <see cref="Destination"/> was changed
        var calculateMoveTimerJob = new CalculateMoveTimerJob
        {
            entityTypeHandle = SystemAPI.GetEntityTypeHandle(),
            moveTimer_CTH_RW = SystemAPI.GetComponentTypeHandle<MoveTimer>(false),
            moveSpeed_CTH_RO = SystemAPI.GetComponentTypeHandle<MoveSpeed>(true),
            worldPosition2D_CTH_RO = SystemAPI.GetComponentTypeHandle<WorldPosition2D>(true),
            destionation_CTH_RO = SystemAPI.GetComponentTypeHandle<Destination>(true),
            ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
            lastSystemVersion = state.LastSystemVersion
        };
        state.Dependency = calculateMoveTimerJob.ScheduleParallelByRef(systemData.movableQuery, state.Dependency);

        var moveJob = new MoveJob
        {
            deltaTime = SystemAPI.Time.DeltaTime,
            ecb = ecbSystem.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
        };
        state.Dependency = moveJob.ScheduleParallelByRef(state.Dependency);
    }
}