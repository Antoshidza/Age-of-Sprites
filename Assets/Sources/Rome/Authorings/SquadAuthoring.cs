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
            var pos = MathHelper.float2(authoring.transform.position);
            AddComponent(entity, LocalTransform2D.FromPosition(pos));
            AddComponent(entity, new LocalToWorld2D());
            AddComponent(entity, new PrevWorldPosition2D { value = pos });
            AddComponent(entity, new SquadSettings { squadResolution = authoring.Resolution, soldierMargin = authoring.SoldierMargin });
            AddComponent(entity, new RequireSoldier { count = authoring.Resolution.x * authoring.Resolution.y });
            _ = AddBuffer<SoldierLink>(entity);
        }
    }

    [FormerlySerializedAs("_resolution")] public int2 Resolution;
    [FormerlySerializedAs("_soldierMargin")] public float2 SoldierMargin;
}