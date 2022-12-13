using NSprites;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class SquadDefaultSettingsAuthoring : MonoBehaviour
{
    private class SquadDefaultSettingsBaker : Baker<SquadDefaultSettingsAuthoring>
    {
        public override void Bake(SquadDefaultSettingsAuthoring authoring)
        {
            if (authoring.SoldierView == null)
                return;

            AddComponent(new SquadDefaultSettings
            {
                soldierPrefab = GetEntity(authoring.SoldierView),
                soldierSize = authoring.VisualSize,
                defaultSettings = new SquadSettings { soldierMargin = authoring.SoldierMargin, squadResolution = authoring.SquadResolution }
            });
        }
    }

    [FormerlySerializedAs("_animResolution ")] public int2 AnimResolution = new(1,1);
    [FormerlySerializedAs("_soldierView")] public GameObject SoldierView;
    [FormerlySerializedAs("_squadResolution")] public int2 SquadResolution;
    [FormerlySerializedAs("_soldierMargin")] public float2 SoldierMargin;

    public float2 VisualSize => SoldierView.TryGetComponent<BaseSpriteRendererAuthoring>(out var authoring) ? authoring.VisualSize : new(1f);
}