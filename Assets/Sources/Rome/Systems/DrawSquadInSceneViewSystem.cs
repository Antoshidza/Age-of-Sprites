#if UNITY_EDITOR
using NSprites;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

[WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
[UpdateInGroup(typeof(PresentationSystemGroup))]
[UpdateAfter(typeof(SpriteRenderingSystem))]
public partial struct DrawSquadInSceneViewSystem : ISystem
{
    private struct EnableSquadDrawing : IComponentData { }

    private EntityQuery _squadQuery;

#if UNITY_EDITOR
    [MenuItem("NSprites/Toggle draw squads for View window")]
    public static void ToggleFrustumCullingSystem()
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var enableSquadDrawingQuery = entityManager.CreateEntityQuery(typeof(EnableSquadDrawing));
        if (enableSquadDrawingQuery.IsEmpty)
            _ = entityManager.AddComponentData(entityManager.CreateEntity(), new EnableSquadDrawing());
        else
            entityManager.DestroyEntity(enableSquadDrawingQuery);
    }
#endif

    public void OnCreate(ref SystemState state)
    {
        _squadQuery = state.GetEntityQuery
        (
            typeof(SquadSettings),
            typeof(LocalTransform2D)
        );
        state.RequireForUpdate<EnableSquadDrawing>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!SystemAPI.TryGetSingleton<SquadDefaultSettings>(out var squadGlobalSettings))
            return;

        var soldierSize = SystemAPI.GetComponent<Scale2D>(squadGlobalSettings.soldierPrefab).value;
        var settings = _squadQuery.ToComponentDataListAsync<SquadSettings>(Allocator.TempJob, out var settings_GatherHandle);
        var transforms = _squadQuery.ToComponentDataListAsync<LocalTransform2D>(Allocator.TempJob, out var poisitions_GatherHandle);

        JobHandle.CombineDependencies(settings_GatherHandle, poisitions_GatherHandle).Complete();

        for (int squadIndex = 0; squadIndex < settings.Length; squadIndex++)
        {
            var squadPos = transforms[squadIndex].Position;
            var setting = settings[squadIndex];
            var squadSize = SquadDefaultSettings.GetSquadSize(setting.squadResolution, soldierSize, setting.soldierMargin);
            var rect = new float2x2(squadPos, squadPos + squadSize);
            Utils.DrawRect(rect, Color.cyan, 0f, Utils.DrawType.Debug);

            var soldierCount = setting.squadResolution.x * setting.squadResolution.y;
            var perSoldierOffset = (2 * setting.soldierMargin + 1f) * soldierSize;

            for (int soldierIndex = 0; soldierIndex < soldierCount; soldierIndex++)
            {
                var rectSize = soldierSize / 16f;
                var soldierPos = new float2(0f, rectSize.y * 1.5f) + squadPos + (perSoldierOffset * new float2(soldierIndex % setting.squadResolution.x + .5f, soldierIndex / setting.squadResolution.x));
                var soldierRect = new float2x2(soldierPos - rectSize, soldierPos + rectSize);
                Utils.DrawRect(soldierRect, Color.green, 0f, Utils.DrawType.Debug);
            }
        }

        settings.Dispose();
        transforms.Dispose();
    }
}
#endif