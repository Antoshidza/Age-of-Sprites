using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    public class PrefabAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new Prefab());
        }
    }
}
