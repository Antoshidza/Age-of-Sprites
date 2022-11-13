using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites
{
    public class Manual2DTransformAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] private bool _readFromTransform;
        [SerializeField] private float2 _worldPosition;
        [SerializeField] private GameObject _parent;
        [SerializeField] private float2 _localPosition;

        private float2 Position => new float2(transform.position.x, transform.position.y);
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (_parent != null)
            {
                dstManager.AddComponentData(entity, new Parent2D { value = conversionSystem.GetPrimaryEntity(_parent) });
                dstManager.AddComponentData(entity, new WorldPosition2D());
                dstManager.AddComponentData(entity, new LocalPosition2D { value = _readFromTransform ? Position : _localPosition });
            }
            else
                dstManager.AddComponentData(entity, new WorldPosition2D { value = _readFromTransform ? Position : _localPosition });
        }
    }
}