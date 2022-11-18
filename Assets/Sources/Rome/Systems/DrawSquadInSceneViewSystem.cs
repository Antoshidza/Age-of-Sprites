using NSprites;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using static UnityEditor.PlayerSettings;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
public struct DrawSquadInSceneViewSystem : ISystem
{
    private EntityQuery _squadQuery;

    public void OnCreate(ref SystemState state)
    {
        _squadQuery = state.GetEntityQuery
        (
            typeof(SquadSettings),
            typeof(WorldPosition2D)
        );
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!state.TryGetSingleton<SquadDefaultSettings>(out var squadGlobalSettings))
            return;

        var settings = _squadQuery.ToComponentDataArrayAsync<SquadSettings>(Allocator.TempJob, out var settings_GatherHandle);
        var positions = _squadQuery.ToComponentDataArrayAsync<WorldPosition2D>(Allocator.TempJob, out var poisitions_GatherHandle);

        JobHandle.CombineDependencies(settings_GatherHandle, poisitions_GatherHandle).Complete();

        for (int squadIndex = 0; squadIndex < settings.Length; squadIndex++)
        {
            var squadPos = positions[squadIndex].value;
            var setting = settings[squadIndex];
            var squadSize = SquadDefaultSettings.GetSquadSize(setting.squadResolution, squadGlobalSettings.soldierSize, setting.soldierMargin);
            var rect = new float2x2(squadPos, squadPos + squadSize);
            Utils.DrawRect(rect, Color.cyan, 0f, Utils.DrawType.Debug);

            var soldierCount = setting.squadResolution.x * setting.squadResolution.y;
            var perSoldierOffset = (2 * setting.soldierMargin + 1f) * squadGlobalSettings.soldierSize;

            for (int soldierIndex = 0; soldierIndex < soldierCount; soldierIndex++)
            {
                var rectSize = squadGlobalSettings.soldierSize / 16f;
                var soldierPos = new float2(0f, rectSize.y * 1.5f) + squadPos + (perSoldierOffset * new float2(soldierIndex % setting.squadResolution.x + .5f, soldierIndex / setting.squadResolution.x));
                var soldierRect = new float2x2(soldierPos - rectSize, soldierPos + rectSize);
                Utils.DrawRect(soldierRect, Color.green, 0f, Utils.DrawType.Debug);
            }
        }


        settings.Dispose();
        positions.Dispose();
    }
}