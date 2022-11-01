using UnityEngine;
using Unity.Entities;
using Unity.Mathematics;
using System.Collections.Generic;

namespace NSprites
{
    public class SpriteRendererAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        static readonly Dictionary<Texture, Material> _overridedMaterials = new();

        public bool ExcludeUnityTransformComponents = true;
        [SerializeField]
        private Sprite _sprite;
        [SerializeField]
        private SpriteRenderData _spriteRenderData;
        public int SortingIndex;
        public bool IncludeInSortingGroup;
        public float2 scale = new(1f);
        [SerializeField]
        private bool _overrideSpriteTexture;
        [SerializeField]
        private float2 _pivot = new(.5f);

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddSpriteRenderComponents(entity);
            _ = dstManager.AddComponentData(entity, new SpriteRendererTag());
            _ = dstManager.AddComponentData(entity, new SpriteSortingIndex());
            _ = dstManager.AddComponentData(entity, new Pivot { value = _pivot });
            _ = dstManager.AddComponentData(entity, new Scale2D { value = new float2(_sprite.bounds.size.x, _sprite.bounds.size.y) * scale });
            var data = _spriteRenderData;
            if (_overrideSpriteTexture)
                data.Material = GetOrCreateOverridedMaterial(_sprite.texture);
            dstManager.AddComponentObject(entity, new SpriteRenderDataToRegistrate { data = data });
        }
        private Material GetOrCreateOverridedMaterial(Texture texture)
        {
            if (!_overridedMaterials.TryGetValue(texture, out var material))
                material = CreateOverridedMaterial(texture);
#if UNITY_EDITOR //for SubScene + domain reload
            else if (material == null)
            {
                _ = _overridedMaterials.Remove(texture);
                material = CreateOverridedMaterial(texture);
            }
#endif
            return material;
        }
        private Material CreateOverridedMaterial(Texture texture)
        {
            var material = new Material(_spriteRenderData.Material);
            material.SetTexture("_MainTex", _sprite.texture);
            _overridedMaterials.Add(texture, material);
            return material;
        }
    }
}
