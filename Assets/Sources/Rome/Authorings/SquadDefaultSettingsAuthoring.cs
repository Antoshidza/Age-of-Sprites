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

            AddComponent(GetEntity(TransformUsageFlags.None), new SquadDefaultSettings
            {
                soldierPrefab = GetEntity(authoring.SoldierView, TransformUsageFlags.None),
                defaultSettings = new SquadSettings { soldierMargin = authoring.SoldierMargin, squadResolution = authoring.SquadResolution }
            });
        }
    }

    public SpriteAnimatedRendererAuthoring SoldierView;
    [FormerlySerializedAs("_squadResolution")] public int2 SquadResolution;
    [FormerlySerializedAs("_soldierMargin")] public float2 SoldierMargin;
}