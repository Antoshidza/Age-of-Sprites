using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace NSprites
{
    public abstract class BaseSpriteRendererAuthoring : MonoBehaviour
    {
        [BakeDerivedTypes]
        private class SpriteRendererBaker : Baker<BaseSpriteRendererAuthoring>
        {
            public override void Bake(BaseSpriteRendererAuthoring authoring)
            {
                _ = DependsOn(authoring._sprite);
                _ = DependsOn(authoring._spriteRenderData.PropertiesSet);
                _ = DependsOn(authoring._spriteRenderData.Material);

                if (!authoring._disableSorting)
                {
                    AddComponent(new VisualSortingTag());
                    AddComponent(new SortingIndex { value = authoring._sortingIndex });
                    AddSharedComponent(new SortingLayer { index = authoring._sortingLayer });

                    if (authoring._staticSorting)
                        AddComponent(new SortingStaticTag());
                }
                AddComponent(new SortingValue());

                AddComponent(new Pivot { value = authoring._pivot });
                AddComponent(new Scale2D { value = authoring.VisualSize });
                var data = authoring._spriteRenderData;
                if (authoring._overrideSpriteTexture)
                    data.Material = authoring.GetOrCreateOverridedMaterial(authoring._sprite.texture);
                AddComponentObject(new SpriteRenderDataToRegistrate { data = data });

                // prevent appearing default Unity.Transform components on entity
                AddComponent<RemoveDefaultTransformComponentsTag>();

                _ = DependsOn(authoring.transform);
                AddComponentObject(new Transform2DRequest { sourceGameObject = authoring.gameObject });
            }
        }

        public static readonly Dictionary<Texture, Material> _overridedMaterials = new();

        public bool ExcludeUnityTransformComponents = true;
        [FormerlySerializedAs("_sprite")] public Sprite _sprite;
        [FormerlySerializedAs("_spriteRenderData")] public SpriteRenderData _spriteRenderData;
        [FormerlySerializedAs("scale ")] public float2 scale = new(1f);
        [FormerlySerializedAs("_overrideSpriteTexture")] public bool _overrideSpriteTexture;
        [FormerlySerializedAs("_pivot ")] public float2 _pivot = new(.5f);
        [Space]
        [FormerlySerializedAs("_disableSorting")] public bool _disableSorting;
        [Tooltip("Use it when entities exists on the same layer and never changes theirs position / sorting index / layer")]
        [FormerlySerializedAs("_staticSorting")] public bool _staticSorting;
        [FormerlySerializedAs("_sortingIndex")] public int _sortingIndex;
        [FormerlySerializedAs("_sortingLayer")] public int _sortingLayer;

        public virtual float2 VisualSize => new float2(_sprite.bounds.size.x, _sprite.bounds.size.y) * scale;

        protected Material GetOrCreateOverridedMaterial(Texture texture)
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
        protected Material CreateOverridedMaterial(Texture texture)
        {
            var material = new Material(_spriteRenderData.Material);
            material.SetTexture("_MainTex", _sprite.texture);
            _overridedMaterials.Add(texture, material);
            return material;
        }
    }
}