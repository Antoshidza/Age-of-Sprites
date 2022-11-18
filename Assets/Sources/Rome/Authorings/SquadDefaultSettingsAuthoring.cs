using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public class SquadDefaultSettingsAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    [SerializeField] private Sprite _soldierSprite;
    [SerializeField] private int2 _animResolution = new(1,1);

    [SerializeField] private GameObject _soldierView;
    [SerializeField] private int2 _squadResolution;
    [SerializeField] private float2 _soldierMargin;

    public float2 VisualSize => new float2(_soldierSprite.bounds.size.x, _soldierSprite.bounds.size.y) / _animResolution;

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        if (_soldierView == null || _soldierSprite == null)
            return;

        _ = dstManager.AddComponentData(entity, new SquadDefaultSettings
        {
            soldierPrefab = conversionSystem.GetPrimaryEntity(_soldierView),
            soldierSize = VisualSize,
            defaultSettings = new SquadSettings { soldierMargin = _soldierMargin, squadResolution = _squadResolution }
        });
    }

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        referencedPrefabs.Add(_soldierView);
    }
}