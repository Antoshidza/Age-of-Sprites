using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class FactoryAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField] private GameObject _prefab;
    [SerializeField] private float2 _spawnOffset;
    [SerializeField] private float _duration = 1f;
    [SerializeField] private int _spawnCount = 1;
    [SerializeField] private bool _randomInitialDuration;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _ = dstManager.AddComponentData(entity, new FactoryData 
        {
            prefab = conversionSystem.GetPrimaryEntity(_prefab),
            instantiatePos = new float2(transform.position.x, transform.position.y) + _spawnOffset,
            count = _spawnCount,
            duration = _duration
        });
        _ = dstManager.AddComponentData(entity, new FactoryTimer { value = _randomInitialDuration ? UnityEngine.Random.Range(0f, _duration) : _duration });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(_prefab);
    }
}