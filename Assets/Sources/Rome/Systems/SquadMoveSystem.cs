using NSprites;
using System.Runtime.CompilerServices;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

[BurstCompile]
public partial struct SquadMoveSystem : ISystem
{
    [BurstCompile]
    private struct MoveJob : IJobChunk
    {
        public float2 SoldierSize;
        [ReadOnly] public BufferTypeHandle<SoldierLink> SoldierLink_BTH_RO;
        [ReadOnly] public ComponentTypeHandle<LocalToWorld2D> LTW2D_CTH_RO;
        [ReadOnly] public ComponentTypeHandle<SquadSettings> SquadSettings_CTH_RO;
        public ComponentTypeHandle<PrevWorldPosition2D> PrevPos_CTH_RW;
        [NativeDisableParallelForRestriction][WriteOnly] public ComponentLookup<Destination> Destination_CL_WO;
        public uint LastSystemVersion;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void MoveSoldiers(in SquadSettings squadSettings, in float2 soldierSize, in DynamicBuffer<SoldierLink> soldiersBuffer, in float2 pos, ref ComponentLookup<Destination> destination_CL_WO)
        {
            var perSoldierOffset = (2 * squadSettings.soldierMargin + 1f) * soldierSize;

            for (int soldierIndex = 0; soldierIndex < soldiersBuffer.Length; soldierIndex++)
                destination_CL_WO[soldiersBuffer[soldierIndex].entity] = new Destination { value = pos + (perSoldierOffset * new float2(soldierIndex % squadSettings.squadResolution.x + .5f, soldierIndex / squadSettings.squadResolution.x)) };
        }

        public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
        {
            var ltw2D = chunk.GetNativeArray(ref LTW2D_CTH_RO);
            var soldierBufferAccessor = chunk.GetBufferAccessor(ref SoldierLink_BTH_RO);

            if (chunk.DidChange(ref LTW2D_CTH_RO, LastSystemVersion))
            {
                var prevPositions = chunk.GetNativeArray(ref PrevPos_CTH_RW);
                var squadSettingsArray = chunk.GetNativeArray(ref SquadSettings_CTH_RO);
                
                for (int squadIndex = 0; squadIndex < ltw2D.Length; squadIndex++)
                {
                    var pos = ltw2D[squadIndex].Position;
                    if (math.any(pos != prevPositions[squadIndex].value))
                    {
                        MoveSoldiers(squadSettingsArray[squadIndex], SoldierSize, soldierBufferAccessor[squadIndex], pos, ref Destination_CL_WO);
                        prevPositions[squadIndex] = new PrevWorldPosition2D { value = pos };
                    }
                }
            }
            else if (chunk.DidChange(ref SoldierLink_BTH_RO, LastSystemVersion))
            {
                var squadSettingsArray = chunk.GetNativeArray(ref SquadSettings_CTH_RO);
                for (int squadIndex = 0; squadIndex < ltw2D.Length; squadIndex++)
                    MoveSoldiers(squadSettingsArray[squadIndex], SoldierSize, soldierBufferAccessor[squadIndex], ltw2D[squadIndex].Position, ref Destination_CL_WO);
            }
        }
    }
    
    [BurstCompile]
    private partial struct MoveOnChangeGlobalSettingsJob : IJobEntity
    {
        public float2 SoldierSize;
        [NativeDisableParallelForRestriction] public ComponentLookup<Destination> Destination_CL;

        private void Execute(in LocalToWorld2D ltw, in DynamicBuffer<SoldierLink> soldiersBuffer, in SquadSettings squadSettings)
        {
            MoveJob.MoveSoldiers(squadSettings, SoldierSize, soldiersBuffer, ltw.Position, ref Destination_CL);
        }
    }
    
    private struct SystemData : IComponentData
    {
        public SquadDefaultSettings PrevSquadSettings;
        public EntityQuery SquadQuery;
    }
    
    [BurstCompile]
    public void OnCreate(ref SystemState state)
    {
        var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
            .WithAll<SoldierLink, LocalToWorld2D, PrevWorldPosition2D, SquadSettings>();

        _ = state.EntityManager.AddComponentData(state.SystemHandle, new SystemData { SquadQuery = state.GetEntityQuery(queryBuilder) });

        queryBuilder.Dispose();
    }
    
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<SquadDefaultSettings>(out var squadDefaultSettings))
            return;

        var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
        var soldierSize = SystemAPI.GetComponent<Scale2D>(squadDefaultSettings.soldierPrefab).value;

        if (systemData.PrevSquadSettings != squadDefaultSettings)
        {
            systemData.PrevSquadSettings = squadDefaultSettings;
            SystemAPI.SetSingleton(systemData);

            var moveOnSettingChangeJob = new MoveOnChangeGlobalSettingsJob
            {
                Destination_CL = SystemAPI.GetComponentLookup<Destination>(false),
                SoldierSize = soldierSize
            };
            state.Dependency = moveOnSettingChangeJob.ScheduleParallelByRef(state.Dependency);
        }
        else
        {
            var moveJob = new MoveJob
            {
                LastSystemVersion = state.LastSystemVersion,
                SoldierSize = soldierSize,
                SquadSettings_CTH_RO = SystemAPI.GetComponentTypeHandle<SquadSettings>(true),
                LTW2D_CTH_RO = SystemAPI.GetComponentTypeHandle<LocalToWorld2D>(true),
                Destination_CL_WO = SystemAPI.GetComponentLookup<Destination>(false),
                PrevPos_CTH_RW = SystemAPI.GetComponentTypeHandle<PrevWorldPosition2D>(false),
                SoldierLink_BTH_RO = SystemAPI.GetBufferTypeHandle<SoldierLink>(true)
            };
            state.Dependency = moveJob.ScheduleParallelByRef(systemData.SquadQuery, state.Dependency);
        }
    }
}