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
        [NativeDisableParallelForRestriction][WriteOnly] public ComponentLookup<Destination> destination_CL_WO;
        public uint lastSystemVersion;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MoveSoldiers(in SquadSettings squadSettings, in float2 soldierSize, in DynamicBuffer<SoldierLink> soldiersBuffer, in float2 pos, ref ComponentLookup<Destination> destination_CL_WO)
        {
            var perSoldierOffset = (2 * squadSettings.soldierMargin + 1f) * soldierSize;

            for (int soldierIndex = 0; soldierIndex < soldiersBuffer.Length; soldierIndex++)
                destination_CL_WO[soldiersBuffer[soldierIndex].entity] = new Destination { value = pos + (perSoldierOffset * new float2(soldierIndex % squadSettings.squadResolution.x + .5f, soldierIndex / squadSettings.squadResolution.x)) };
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var positions = chunk.GetNativeArray(ref worldPos2D_CTH_RO);
            var soldierBufferAccessor = chunk.GetBufferAccessor(ref soldierLink_BTH_RO);

            if (chunk.DidChange(ref worldPos2D_CTH_RO, lastSystemVersion))
            {
                var prevPositions = chunk.GetNativeArray(ref prevPos_CTH_RW);

                var squadSettingsArray = chunk.GetNativeArray(ref squadSettings_CTH_RO);
                for (int squadIndex = 0; squadIndex < positions.Length; squadIndex++)
                {
                    var pos = positions[squadIndex].value;
                    if (math.any(pos != prevPositions[squadIndex].value))
                    {
                        MoveSoldiers(squadSettingsArray[squadIndex], squadDefaultSettings.soldierSize, soldierBufferAccessor[squadIndex], pos, ref destination_CL_WO);
                        prevPositions[squadIndex] = new PrevWorldPosition2D { value = pos };
                    }
                }
            }
            else if (chunk.DidChange(ref soldierLink_BTH_RO, lastSystemVersion))
            {
                var squadSettingsArray = chunk.GetNativeArray(ref squadSettings_CTH_RO);
                for (int squadIndex = 0; squadIndex < positions.Length; squadIndex++)
                    MoveSoldiers(squadSettingsArray[squadIndex], squadDefaultSettings.soldierSize, soldierBufferAccessor[squadIndex], positions[squadIndex].value, ref destination_CL_WO);
            }
        }
    }
    [BurstCompile]
    private partial struct MoveOnChangeGlobalSettingsJob : IJobEntity
    {
        public SquadDefaultSettings squadDefaultSettings;
        [NativeDisableParallelForRestriction] public ComponentLookup<Destination> destination_CL;

        private void Execute(in WorldPosition2D pos, in DynamicBuffer<SoldierLink> soldiersBuffer, in SquadSettings squadSettings)
        {
            MoveJob.MoveSoldiers(squadSettings, squadDefaultSettings.soldierSize, soldiersBuffer, pos.value, ref destination_CL);
        }
    }
    private struct SystemData : IComponentData
    {
        public SquadDefaultSettings prevSquadSettings;
        public EntityQuery squadQuery;
    }

    public void OnCreate(ref SystemState state)
    {
        var systemData = new SystemData
        {
            squadQuery = state.GetEntityQuery(typeof(SoldierLink))
        };
        _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<SquadDefaultSettings>(out var squadDefaultSettings))
            return;

        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);

        if (systemData.prevSquadSettings != squadDefaultSettings)
        {
            systemData.prevSquadSettings = squadDefaultSettings;

            var moveOnSettingChangeJob = new MoveOnChangeGlobalSettingsJob
            {
                destination_CL = SystemAPI.GetComponentLookup<Destination>(false),
                squadDefaultSettings = squadDefaultSettings
            };
            state.Dependency = moveOnSettingChangeJob.ScheduleParallelByRef(state.Dependency);
        }
        else
        {
            var moveJob = new MoveJob
            {
                lastSystemVersion = state.LastSystemVersion,
                squadDefaultSettings = squadDefaultSettings,
                squadSettings_CTH_RO = SystemAPI.GetComponentTypeHandle<SquadSettings>(true),
                worldPos2D_CTH_RO = SystemAPI.GetComponentTypeHandle<WorldPosition2D>(true),
                destination_CL_WO = SystemAPI.GetComponentLookup<Destination>(false),
                prevPos_CTH_RW = SystemAPI.GetComponentTypeHandle<PrevWorldPosition2D>(false),
                soldierLink_BTH_RO = SystemAPI.GetBufferTypeHandle<SoldierLink>(true)
            };
            state.Dependency = moveJob.ScheduleParallelByRef(systemData.squadQuery, state.Dependency);
        }
    }
}