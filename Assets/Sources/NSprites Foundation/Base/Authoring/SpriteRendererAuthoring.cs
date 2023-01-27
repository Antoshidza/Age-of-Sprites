using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace NSprites
{
    /// <summary>
    /// Adds basic render components such as <see cref="MainTexST"/>, <see cref="Scale2D"/>, <see cref="Pivot"/>.
    /// Optionally adds sorting components, removes built-in 3D transforms and adds 2D transforms.
    /// </summary>
    public class SpriteRendererAuthoring : SpriteRendererAuthoringBase
    {
        private class Baker : Baker<SpriteRendererAuthoring>
        {
            public override void Bake(SpriteRendererAuthoring authoring)
            {
                BakeSpriteRender
                (
                    this,
                    authoring,
                    NSpritesUtils.GetTextureST(authoring._sprite),
                    authoring._pivot,
                    authoring.VisualSize,
                    removeDefaultTransform: authoring._excludeUnityTransformComponents
                );
                if(!authoring._disableSorting)
                    BakeSpriteSorting
                    (
                        this,
                        authoring._sortingIndex,
                        authoring._sortingLayer,
                        authoring._staticSorting
                    );
            }
        }
        
        [FormerlySerializedAs("_sprite")][SerializeField] protected Sprite _sprite;
        [FormerlySerializedAs("_spriteRenderData")][SerializeField] protected SpriteRenderData _spriteRenderData;
        [FormerlySerializedAs("_overrideSpriteTexture")][SerializeField] protected bool _overrideSpriteTexture;
        [FormerlySerializedAs("ExcludeUnityTransformComponents")] [SerializeField] protected bool _excludeUnityTransformComponents = true;
        [FormerlySerializedAs("scale ")][SerializeField] protected float2 _scale = new(1f);
        [FormerlySerializedAs("_pivot ")][SerializeField] protected float2 _pivot = new(.5f);
        [Space]
        [FormerlySerializedAs("DisableSorting")] [Tooltip("Won't add any sorting related components")] protected bool _disableSorting;
        [FormerlySerializedAs("StaticSorting")] [Tooltip("Use it when entities exists on the same layer and never changes theirs position / sorting index / layer")] protected bool _staticSorting;
        [FormerlySerializedAs("SortingIndex")] protected int _sortingIndex;
        [FormerlySerializedAs("SortingLayer")] protected int _sortingLayer;

        public static float2 GetSpriteSize(Sprite sprite) => new(sprite.bounds.size.x, sprite.bounds.size.y);
        public virtual float2 VisualSize => GetSpriteSize(_sprite) * _scale;

        public static void BakeSpriteRender<TAuthoring>(Baker<TAuthoring> baker, TAuthoring authoring, in float4 mainTexST, in float2 pivot, in float2 scale, bool removeDefaultTransform = true, bool add2DTransform = true)
            where TAuthoring : MonoBehaviour
        {
            baker.AddComponent(new MainTexST { value = mainTexST });
            baker.AddComponent(new Pivot { value = pivot });
            baker.AddComponent(new Scale2D { value = scale });
            
            if(removeDefaultTransform)
                baker.AddComponent<RemoveDefaultTransformComponentsTag>();
            if (add2DTransform)
            {
                baker.AddComponentObject(new Transform2DRequest { sourceGameObject = authoring.gameObject });
                baker.DependsOn(authoring.transform);
            }
        }

        public static void BakeSpriteSorting<TAuthoring>(Baker<TAuthoring> baker, int sortingIndex, int sortingLayer, bool staticSorting = false)
            where TAuthoring : MonoBehaviour
        {
            baker.AddComponent<VisualSortingTag>();
            baker.AddComponent<SortingValue>();
            baker.AddComponent(new SortingIndex { value = sortingIndex });
            baker.AddSharedComponent(new SortingLayer { index = sortingLayer });
            if(staticSorting)
                baker.AddComponent<SortingStaticTag>();
        }
        
        private static readonly Dictionary<Texture, Material> _overridedMaterials = new();
        
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

        protected override SpriteRenderData RenderData
        {
            get
            {
                if (_overrideSpriteTexture)
                    _spriteRenderData.Material = GetOrCreateOverridedMaterial(_sprite.texture);
                return _spriteRenderData;
            }
        }
    }
}
