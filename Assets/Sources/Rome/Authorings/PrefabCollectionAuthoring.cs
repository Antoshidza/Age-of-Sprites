using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

public class PrefabCollectionAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField] private GameObject[] _prefabs;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (_prefabs == null)
            return;

        var prefabLinkBuffer = dstManager.AddBuffer<PrefabLink>(entity);
        prefabLinkBuffer.Capacity = _prefabs.Length;
        for (int i = 0; i < _prefabs.Length; i++)
            _ = prefabLinkBuffer.Add(new PrefabLink { link = conversionSystem.GetPrimaryEntity(_prefabs[i]) });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.AddRange(_prefabs);
    }
}