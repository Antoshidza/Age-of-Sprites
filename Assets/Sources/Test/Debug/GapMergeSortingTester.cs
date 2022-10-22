using System.Collections.Generic;
//using TonyMax.Entities.Rendering.Sprites;
//using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class GapMergeSortingTester : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField]
    private int _sortCount;

    [SerializeField]
    private GameObject _spritePrefab;
    private Entity _spriteEntity;

    private void Update()
    {
        //TestSpriteSort();

        //if(Input.GetKeyDown(KeyCode.Q))
        //    World.DefaultGameObjectInjectionWorld.EntityManager.Instantiate(_spriteEntity);
    }
    //private void TestSpriteSort()
    //{
    //    var sortingDataArray = new NativeArray<SpriteRenderingSystem.SortingData>(_sortCount, Allocator.TempJob);
    //    for(int i = 0; i < sortingDataArray.Length; i++)
    //    {
    //        var isChild = i > 0 && UnityEngine.Random.Range(0f, 1f) > .5f;
    //        sortingDataArray[i] = new SpriteRenderingSystem.SortingData()
    //        {
    //            id = i,
    //            groupID = isChild ? UnityEngine.Random.Range(0, i) : -1,
    //            position = UnityEngine.Random.Range(0f, 10f),
    //            archetypeIncludedIndex = UnityEngine.Random.Range(0, 5)
    //        };
    //    }

    //    void PrintArray()
    //    {
    //        var str = string.Empty;
    //        for(int i = 0; i < sortingDataArray.Length; i++)
    //            str += $"{i}. {sortingDataArray[i].archetypeIncludedIndex}\n";
    //        Debug.Log(str);
    //    }

    //    //PrintArray();

    //    //var batchSize = 128;
    //    //var stream = new NativeStream((int)math.ceil((float)sortingDataArray.Length / batchSize), Allocator.TempJob);
    //    //var handle = new SpriteRenderingSystem.GatherRenderGroupsToStreamJob()
    //    //{
    //    //    sortingData = sortingDataArray,
    //    //    writer = stream.AsWriter(),
    //    //    batchSize = batchSize,
    //    //}.ScheduleBatch(sortingDataArray.Length, batchSize);
    //    //handle.Complete();

    //    //var groups = stream.ToNativeArray<int2>(Allocator.Temp);

    //    //var str = string.Empty;
    //    //var sum = 0;
    //    //for(int i = 0; i < groups.Length; i++)
    //    //{
    //    //    str += $"{groups[i]}\n";
    //    //    sum += groups[i].y;
    //    //}
    //    //Debug.Log(str);

    //    //if(sum != sortingDataArray.Length)
    //    //    Debug.LogError($"sum {sum} != length {sortingDataArray.Length}");

    //    sortingDataArray.Dispose();
    //    //stream.Dispose();
    //}

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        _spriteEntity = conversionSystem.GetPrimaryEntity(_spritePrefab);
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(_spritePrefab);
    }
}
