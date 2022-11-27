using NSprites;
using System.Runtime.CompilerServices;
using Unity.Burst;
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

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            if (chunk.DidChange(destionation_CTH_RO, lastSystemVersion)
                || chunk.DidChange(moveSpeed_CTH_RO, lastSystemVersion))
            {
                var entities = chunk.GetNativeArray(entityTypeHandle);
                var worldPositions = chunk.GetNativeArray(worldPosition2D_CTH_RO);
                var moveSpeeds = chunk.GetNativeArray(moveSpeed_CTH_RO);
                var destionations = chunk.GetNativeArray(destionation_CTH_RO);
                var timers = chunk.GetNativeArray(moveTimer_CTH_RW);

                if (chunk.Has(moveTimer_CTH_RW))
                    for (int entityIndex = 0; entityIndex < worldPositions.Length; entityIndex++)
                        timers[entityIndex] = new MoveTimer { remainingTime = GetRamainingTime(worldPositions[entityIndex].value, destionations[entityIndex].value, moveSpeeds[entityIndex].value) };
                else
                    for (int entityIndex = 0; entityIndex < worldPositions.Length; entityIndex++)
                    {
                        var distance = math.length(destionations[entityIndex].value - worldPositions[entityIndex].value);
                        if(distance > threshold)
                            ecb.AddComponent(firstEntityIndex + entityIndex, entities[entityIndex], new MoveTimer { remainingTime = GetRamainingTime(distance, moveSpeeds[entityIndex].value) });
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
    #endregion

    private EntityCommandBufferSystem _ecbSystem;
    private EntityQuery _movableQuery;

    public void OnCreate(ref SystemState state)
    {
        _ecbSystem = state.World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        _movableQuery = state.GetEntityQuery
        (
            ComponentType.ReadOnly<MoveSpeed>(),
            ComponentType.ReadOnly<WorldPosition2D>(),
            ComponentType.ReadOnly<Destination>()
        );
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = state.Time.DeltaTime;

        var oecbTimerJb = _ecbSystem.CreateCommandBuffer();

        /// [re]calculate <see cref="MoveTimer"/> if <see cref="MoveSpeed"/> or <see cref="Destination"/> was changed
        var calculateMoveTimerJob = new CalculateMoveTimerJob
        {
            entityTypeHandle = state.GetEntityTypeHandle(),
            moveTimer_CTH_RW = state.GetComponentTypeHandle<MoveTimer>(false),
            moveSpeed_CTH_RO = state.GetComponentTypeHandle<MoveSpeed>(true),
            worldPosition2D_CTH_RO = state.GetComponentTypeHandle<WorldPosition2D>(true),
            destionation_CTH_RO = state.GetComponentTypeHandle<Destination>(true),
            ecb = oecbTimerJb.AsParallelWriter(),
            lastSystemVersion = state.LastSystemVersion
        };
        state.Dependency = calculateMoveTimerJob.ScheduleParallelByRef(_movableQuery, state.Dependency);

        var ecbMoveJob = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

        state.Dependency = state.Entities.ForEach((Entity entity, int entityInQueryIndex, ref WorldPosition2D pos, ref MoveTimer timer, in Destination destination) =>
        {
            var remainingDelta = math.max(timer.remainingTime, deltaTime - timer.remainingTime);
            // move pos in a direction of current destination by passed frac of whole remaining move time
            pos.value += (destination.value - pos.value) * deltaTime / remainingDelta;
            timer.remainingTime = math.max(0, timer.remainingTime - deltaTime);

            if (timer.remainingTime == 0f)
                ecbMoveJob.RemoveComponent<MoveTimer>(entityInQueryIndex, entity);
        }).ScheduleParallel(state.Dependency);

        _ecbSystem.AddJobHandleForProducer(state.Dependency);
    }
}