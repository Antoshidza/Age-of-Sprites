using NSprites;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SquadAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField] private int2 _resolution;
    [SerializeField] private float2 _soldierMargin;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var pos = new float2(transform.position.x, transform.position.y);
        _ = dstManager.AddComponentData(entity, new WorldPosition2D { value = pos });
        _ = dstManager.AddComponentData(entity, new PrevWorldPosition2D { value = pos });
        _ = dstManager.AddComponentData(entity, new SquadSettings { squadResolution = _resolution, soldierMargin = _soldierMargin });
        _ = dstManager.AddComponentData(entity, new RequireSoldier { count = _resolution.x * _resolution.y });
        _ = dstManager.AddBuffer<SoldierLink>(entity);
    }
}