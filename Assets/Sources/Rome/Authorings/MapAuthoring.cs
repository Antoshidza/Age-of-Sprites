using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public class MapAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField] private Color _gizmoColor = Color.green;
    [Space]

    [SerializeField] private float2x2 _rect;
    [SerializeField] private int _rockCount;
    [SerializeField] private GameObject[] _rockPrefabs;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (_rockPrefabs == null)
            return;

        var rockCollectionEntity = conversionSystem.CreateAdditionalEntity(this);
        var rockBuffer = dstManager.AddBuffer<PrefabLink>(rockCollectionEntity);
        rockBuffer.Capacity = _rockPrefabs.Length;
        for (int i = 0; i < _rockPrefabs.Length; i++)
            _ = rockBuffer.Add(new PrefabLink { link = conversionSystem.GetPrimaryEntity(_rockPrefabs[i]) });

        _ = dstManager.AddComponentData(entity, new MapSettings
        {
            rockCollectionLink = rockCollectionEntity,
            rockCount = _rockCount,
            size = _rect
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.AddRange(_rockPrefabs);
    }

    private void OnDrawGizmosSelected()
    {
        Utils.DrawRect(_rect, _gizmoColor);
    }
}