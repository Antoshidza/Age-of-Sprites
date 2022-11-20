using UnityEngine;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Mathematics;
using Unity.Entities;
using Random = Unity.Mathematics.Random;
using NSprites;

public class SpawnRandomSpritesAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField] private int _spawnCount = 1;
    [SerializeField] private GameObject[] _prefabs;
    [SerializeField] private float2x2 _spawnBounds = new(-100f, -100f, 100f, 100f);

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if(_prefabs == null)
            return;

        var prefabEntities = new NativeArray<Entity>(_prefabs.Length, Allocator.Temp);
        for (int prefabIndex = 0; prefabIndex < _prefabs.Length; prefabIndex++)
            prefabEntities[prefabIndex] = conversionSystem.GetPrimaryEntity(_prefabs[prefabIndex]);

        var rand = new Random((uint)System.DateTime.Now.Ticks);
        for (int i = 0; i < _spawnCount; i++)
        {
            var newEntity = dstManager.Instantiate(prefabEntities[rand.NextInt(0, prefabEntities.Length)]);
            dstManager.SetComponentData(newEntity, new WorldPosition2D { value = rand.NextFloat2(_spawnBounds.c0, _spawnBounds.c1) });
            dstManager.AddComponentData(newEntity, new RandomColor());
        }
    }
    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.AddRange(_prefabs);
    }
}
