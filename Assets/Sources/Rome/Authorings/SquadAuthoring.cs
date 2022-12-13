using NSprites;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class SquadAuthoring : MonoBehaviour
{
    private class SquadBaker : Baker<SquadAuthoring>
    {
        public override void Bake(SquadAuthoring authoring)
        {
            var pos = new float2(authoring.transform.position.x, authoring.transform.position.y);
            AddComponent(new WorldPosition2D { value = pos });
            AddComponent(new PrevWorldPosition2D { value = pos });
            AddComponent(new SquadSettings { squadResolution = authoring.Resolution, soldierMargin = authoring.SoldierMargin });
            AddComponent(new RequireSoldier { count = authoring.Resolution.x * authoring.Resolution.y });
            _ = AddBuffer<SoldierLink>();
        }
    }

    [FormerlySerializedAs("_resolution")] public int2 Resolution;
    [FormerlySerializedAs("_soldierMargin")] public float2 SoldierMargin;
}