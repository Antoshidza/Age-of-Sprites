using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;

public class MoveAreaAuthoring : MonoBehaviour, IConvertGameObjectToEntity
{
    [SerializeField]
    private float2 _area;
    [SerializeField]
    private bool2 _randomizeComponents;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _area = new float2
        (
            _randomizeComponents.x ? UnityEngine.Random.Range(0f,1f) : _area.x,
            _randomizeComponents.y ? UnityEngine.Random.Range(0f, 1f) : _area.y
        );
        dstManager.AddComponentData
        (
            entity,
            new MoveAround
            {
                area = _area,
                startPosition = new float2(transform.position.x, transform.position.y),
                timeOffset = UnityEngine.Random.Range(0f, 1f)
            }
        );
    }
}
