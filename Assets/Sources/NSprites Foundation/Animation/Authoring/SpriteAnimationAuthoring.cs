using System.Linq;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace NSprites
{
    /// <summary>
    /// Advanced <see cref="SpriteRendererAuthoring"/> which also bakes animation data as blob asset and adds animation components.
    /// </summary>
    public class SpriteAnimationAuthoring : SpriteRendererAuthoring
    {
        private class Baker : Baker<SpriteAnimationAuthoring>
        {
            public override void Bake(SpriteAnimationAuthoring authoring)
            {
                BakeSpriteAnimation(this, authoring.AnimationSet, authoring.InitialAnimationIndex);

                var initialAnimData = authoring.AnimationSet.Animations.ElementAt(authoring.InitialAnimationIndex).data;
                var initialAnimMainTexST = (float4)NSpritesUtils.GetTextureST(initialAnimData.SpriteSheet);

                BakeSpriteRender
                (
                    this,
                    authoring,
                    new float4(new float2(initialAnimMainTexST.xy / initialAnimData.FrameCount), 0f),
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

        [Header("Animation Data")]
        [FormerlySerializedAs("_animationSet")] public SpriteAnimationSet AnimationSet;
        [FormerlySerializedAs("_initialAnimationIndex")] public int InitialAnimationIndex;

        public override float2 VisualSize
        {
            get
            {
                var animationData = AnimationSet.Animations.ElementAt(InitialAnimationIndex).data;
                return GetSpriteSize(animationData.SpriteSheet) / animationData.FrameCount;
            }
        }

        public static void BakeSpriteAnimation<TAuthoring>(Baker<TAuthoring> baker, SpriteAnimationSet animationSet, int initialAnimationIndex = 0)
            where TAuthoring : MonoBehaviour
        {
                baker.DependsOn(animationSet);

                if (animationSet == null)
                    return;

                if (initialAnimationIndex >= animationSet.Animations.Count || initialAnimationIndex < 0)
                {
                    Debug.LogError($"Initial animation index {initialAnimationIndex} can't be less than 0 or great/equal to animation count {animationSet.Animations.Count}");
                    return;
                }
                
                #region create animation blob asset
                var blobBuilder = new BlobBuilder(Allocator.Temp); //can't use `using` keyword because there is extension which use this + ref
                ref var root = ref blobBuilder.ConstructRoot<BlobArray<SpriteAnimationBlobData>>();
                var animations = animationSet.Animations;
                var animationArray = blobBuilder.Allocate(ref root, animations.Count);

                var animIndex = 0;
                foreach (var anim in animations)
                {
                    var animData = anim.data;
                    var animationDuration = 0f;
                    for (int i = 0; i < animData.FrameDurations.Length; i++)
                        animationDuration += animData.FrameDurations[i];

                    animationArray[animIndex] = new SpriteAnimationBlobData
                    {
                        ID = Animator.StringToHash(anim.name),
                        GridSize = animData.FrameCount,
                        MainTexSTOnAtlas = NSpritesUtils.GetTextureST(animData.SpriteSheet),
                        Scale2D = new float2(animData.SpriteSheet.bounds.size.x, animData.SpriteSheet.bounds.size.y),
                        AnimationDuration = animationDuration
                        // FrameDuration - allocate lately
                    };

                    var durations = blobBuilder.Allocate(ref animationArray[animIndex].FrameDurations, animData.FrameDurations.Length);
                    for (int di = 0; di < durations.Length; di++)
                        durations[di] = animData.FrameDurations[di];

                    animIndex++;
                }

                var blobAssetReference = blobBuilder.CreateBlobAssetReference<BlobArray<SpriteAnimationBlobData>>(Allocator.Persistent);
                baker.AddBlobAsset(ref blobAssetReference, out _);
                blobBuilder.Dispose();
                #endregion

                ref var initialAnim = ref blobAssetReference.Value[initialAnimationIndex];

                baker.AddComponent(new AnimationSetLink { value = blobAssetReference });
                baker.AddComponent(new AnimationIndex { value = initialAnimationIndex });
                baker.AddComponent(new AnimationTimer { value = initialAnim.FrameDurations[0] });
                baker.AddComponent<FrameIndex>();
                
                baker.AddComponent(new MainTexSTInitial { value = initialAnim.MainTexSTOnAtlas });
        }
    }
}