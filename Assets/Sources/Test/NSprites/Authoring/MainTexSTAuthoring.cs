using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    public class MainTexSTAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField]
        private Sprite _sprite;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new MainTexST { value = NSpritesUtils.GetTextureST(_sprite) });
            dstManager.AddComponentData(entity, new MainTexSTInitial { value = NSpritesUtils.GetTextureST(_sprite) });
        }
    }
}
