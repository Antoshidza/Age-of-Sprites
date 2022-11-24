using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

public class SpawnRandomSpritesAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField] private int _totalCount = 1;
    [SerializeField] private int _startSpawnCount = 1;
    [SerializeField] private int _spawnAcceleration = 1;
    [SerializeField] private GameObject[] _prefabs;
    [SerializeField] private float2x2 _spawnBounds;
    [SerializeField] private float _timerBeforeSpawn;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if(_prefabs == null)
            return;

        var prefabEntities = new NativeArray<Entity>(_prefabs.Length, Allocator.Temp);
        for (int prefabIndex = 0; prefabIndex < _prefabs.Length; prefabIndex++)
            prefabEntities[prefabIndex] = conversionSystem.GetPrimaryEntity(_prefabs[prefabIndex]);

        var prefabBuffer = dstManager.AddBuffer<PrefabLink>(entity);
        prefabBuffer.AddRange(prefabEntities.Reinterpret<PrefabLink>());

        _ = dstManager.AddComponentData(entity, new SpawnerData
        {
            totalCount = _totalCount,
            countPerSpawn = _startSpawnCount,
            spawnAcceleration = _spawnAcceleration,
            spawnBounds = _spawnBounds
        });

        _ = dstManager.AddComponentData(entity, new FactoryTimer { value = _timerBeforeSpawn });
    }
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.AddRange(_prefabs);
    }
}
