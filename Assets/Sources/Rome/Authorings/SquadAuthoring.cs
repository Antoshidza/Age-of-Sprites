using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SquadAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField] private Sprite _soldierSprite;
    [SerializeField] private int2 _animResolution = new(1,1);

    [SerializeField] private GameObject _soldierView;
    [SerializeField] private int2 _squadResolution;
    [SerializeField] private float2 _soldierMargin;

    public float2 VisualSize => new float2(_soldierSprite.bounds.size.x, _soldierSprite.bounds.size.y) / _animResolution;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        dstManager.AddComponentData(entity, new SquadSettings
        {
            soldierPrefab = conversionSystem.GetPrimaryEntity(_soldierView),
            soldierMargin = _soldierMargin,
            soldierSize = VisualSize,
            squadResolution = _squadResolution,
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(_soldierView);
    }
}
