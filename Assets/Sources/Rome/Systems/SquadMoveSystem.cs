using NSprites;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

[BurstCompile]
public partial struct SquadMoveSystem : ISystem
{
    [BurstCompile]
    private struct MoveJob : IJobChunk
    {
        public SquadDefaultSettings squadDefaultSettings;
        [ReadOnly] public BufferTypeHandle<SoldierLink> soldierLink_BTH_RO;
        [ReadOnly] public ComponentTypeHandle<WorldPosition2D> worldPos2D_CTH_RO;
        [ReadOnly] public ComponentTypeHandle<SquadSettings> squadSettings_CTH_RO;
        public ComponentTypeHandle<PrevWorldPosition2D> prevPos_CTH_RW;
        [NativeDisableParallelForRestriction][WriteOnly] public ComponentDataFromEntity<Destination> destination_CDFE_WO;
        public uint lastSystemVersion;

        public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
        {
            var positions = chunk.GetNativeArray(worldPos2D_CTH_RO);
            var soldierBufferAccessor = chunk.GetBufferAccessor(soldierLink_BTH_RO);

            if (chunk.DidChange(worldPos2D_CTH_RO, lastSystemVersion))
            {
                var prevPositions = chunk.GetNativeArray(prevPos_CTH_RW);

                var squadSettingsArray = chunk.GetNativeArray(squadSettings_CTH_RO);
                for (int squadIndex = 0; squadIndex < positions.Length; squadIndex++)
                {
                    var pos = positions[squadIndex].value;
                    if (math.any(pos != prevPositions[squadIndex].value))
                    {
                        MoveSoldiers(squadSettingsArray[squadIndex], squadDefaultSettings.soldierSize, soldierBufferAccessor[squadIndex], pos, ref destination_CDFE_WO);
                        prevPositions[squadIndex] = new PrevWorldPosition2D { value = pos };
                    }
                }
            }
            else if (chunk.DidChange(soldierLink_BTH_RO, lastSystemVersion))
            {
                var squadSettingsArray = chunk.GetNativeArray(squadSettings_CTH_RO);
                for (int squadIndex = 0; squadIndex < positions.Length; squadIndex++)
                    MoveSoldiers(squadSettingsArray[squadIndex], squadDefaultSettings.soldierSize, soldierBufferAccessor[squadIndex], positions[squadIndex].value, ref destination_CDFE_WO);
            }
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MoveSoldiers(in SquadSettings squadSettings, in float2 soldierSize, in DynamicBuffer<SoldierLink> soldiersBuffer, in float2 pos, ref ComponentDataFromEntity<Destination> destination_CDFE)
        {
            var perSoldierOffset = (2 * squadSettings.soldierMargin + 1f) * soldierSize;

            for (int soldierIndex = 0; soldierIndex < soldiersBuffer.Length; soldierIndex++)
                destination_CDFE[soldiersBuffer[soldierIndex].entity] = new Destination { value = pos + (perSoldierOffset * new float2(soldierIndex % squadSettings.squadResolution.x + .5f, soldierIndex / squadSettings.squadResolution.x)) };
        }
    }

    private SquadDefaultSettings _prevSquadSettings;
    private EntityQuery _squadQuery;

    public void OnCreate(ref SystemState state)
    {
        _squadQuery = state.GetEntityQuery(typeof(SoldierLink));
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var squadDefaultSettings = state.GetSingleton<SquadDefaultSettings>();

        var destination_CDFE = state.GetComponentDataFromEntity<Destination>();

        if (_prevSquadSettings != squadDefaultSettings)
        {
            _prevSquadSettings = squadDefaultSettings;

            state.Dependency = state.Entities
            .WithNativeDisableParallelForRestriction(destination_CDFE)
            .ForEach((in WorldPosition2D pos, in DynamicBuffer<SoldierLink> soldiersBuffer, in SquadSettings squadSettings) =>
            {
                MoveJob.MoveSoldiers(squadSettings, squadDefaultSettings.soldierSize, soldiersBuffer, pos.value, ref destination_CDFE);
            }).ScheduleParallel(state.Dependency);
        }
        else
        {
            var moveJob = new MoveJob
            {
                lastSystemVersion = state.LastSystemVersion,
                squadDefaultSettings = squadDefaultSettings,
                squadSettings_CTH_RO = state.GetComponentTypeHandle<SquadSettings>(true),
                worldPos2D_CTH_RO = state.GetComponentTypeHandle<WorldPosition2D>(true),
                destination_CDFE_WO = state.GetComponentDataFromEntity<Destination>(false),
                prevPos_CTH_RW = state.GetComponentTypeHandle<PrevWorldPosition2D>(false),
                soldierLink_BTH_RO = state.GetBufferTypeHandle<SoldierLink>(true)
            };
            state.Dependency = moveJob.ScheduleParallelByRef(_squadQuery, state.Dependency);
        }
    }
}