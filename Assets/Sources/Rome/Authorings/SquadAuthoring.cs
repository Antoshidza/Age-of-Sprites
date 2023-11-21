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
            var entity = GetEntity(TransformUsageFlags.None);
            var pos = new float3(authoring.transform.position).xy;
            AddComponent(entity, new WorldPosition2D { Value = pos });
            AddComponent(entity, new PrevWorldPosition2D { value = pos });
            AddComponent(entity, new SquadSettings { squadResolution = authoring.Resolution, soldierMargin = authoring.SoldierMargin });
            AddComponent(entity, new RequireSoldier { count = authoring.Resolution.x * authoring.Resolution.y });
            _ = AddBuffer<SoldierLink>(entity);
        }
    }

    [FormerlySerializedAs("_resolution")] public int2 Resolution;
    [FormerlySerializedAs("_soldierMargin")] public float2 SoldierMargin;
}