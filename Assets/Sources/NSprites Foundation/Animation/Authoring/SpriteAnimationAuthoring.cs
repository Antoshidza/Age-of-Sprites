using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

namespace NSprites
{
    [BakeDerivedTypes]
    public class SpriteAnimationAuthoring : BaseSpriteRendererAuthoring
    {
        private class SpriteAnimationBaker : Baker<SpriteAnimationAuthoring>
        {
            public override void Bake(SpriteAnimationAuthoring authoring)
            {
                _ = DependsOn(authoring.AnimationSet);

                if (authoring.AnimationSet == null)
                    return;

                if (authoring.InitialAnimationIndex >= authoring.AnimationSet.Animations.Count)
                    throw new System.Exception($"Initial animation index {authoring.InitialAnimationIndex} can't be great/equal to animation count {authoring.AnimationSet.Animations.Count}");

                #region create blob asset
                var blobBuilder = new BlobBuilder(Allocator.Temp); //can't use using because there is extension which use this + ref
                ref var root = ref blobBuilder.ConstructRoot<BlobArray<SpriteAnimationBlobData>>();
                var animations = authoring.AnimationSet.Animations;
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
                AddBlobAsset(ref blobAssetReference, out _);
                blobBuilder.Dispose();
                #endregion

                ref var initialAnim = ref blobAssetReference.Value[authoring.InitialAnimationIndex];

                AddComponent(new AnimationSetLink { value = blobAssetReference });
                AddComponent(new AnimationIndex { value = authoring.InitialAnimationIndex });
                AddComponent(new AnimationTimer { value = initialAnim.FrameDurations[0] });
                AddComponent(new FrameIndex());

                var frameSize = new float2(initialAnim.MainTexSTOnAtlas.xy / initialAnim.GridSize);
                var framePosition = new int2(0 % initialAnim.GridSize.x, 0 / initialAnim.GridSize.x);
                AddComponent(new MainTexSTInitial { value = initialAnim.MainTexSTOnAtlas });
                AddComponent(new MainTexST { value = new float4(frameSize, initialAnim.MainTexSTOnAtlas.zw + frameSize * framePosition) });
            }
        }

        [Header("Sprite Animation Data")]
        [FormerlySerializedAs("_animationSet")] public SpriteAnimationSet AnimationSet;
        [FormerlySerializedAs("_initialAnimationIndex")] public int InitialAnimationIndex;
    }
}