using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites
{
    public class SpriteAnimationAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] private SpriteAnimationSet _animationSet;
        [SerializeField] private int _initialAnimationIndex;
        private static BlobAssetStore _blobAssetStore;

        private static BlobAssetStore BlobAssetStore => _blobAssetStore ??= new BlobAssetStore();

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (_animationSet == null)
                return;

            if (_initialAnimationIndex >= _animationSet.Animations.Count)
                throw new System.Exception($"Initial animation index {_initialAnimationIndex} can't be great/equal to animation count {_animationSet.Animations.Count}");

            #region create blob asset
            var blobBuilder = new BlobBuilder(Allocator.Temp); //can't use using because there is extension which use this + ref
            ref var root = ref blobBuilder.ConstructRoot<BlobArray<SpriteAnimationBlobData>>();
            var animations = _animationSet.Animations;
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
            _ = BlobAssetStore.AddUniqueBlobAsset(ref blobAssetReference);
            blobBuilder.Dispose();
            #endregion

            ref var initialAnim = ref blobAssetReference.Value[_initialAnimationIndex];

            _ = dstManager.AddComponentData(entity, new AnimationSetLink { value = blobAssetReference });
            _ = dstManager.AddComponentData(entity, new AnimationIndex { value = _initialAnimationIndex });
            _ = dstManager.AddComponentData(entity, new AnimationTimer { value = initialAnim.FrameDurations[0] });
            _ = dstManager.AddComponentData(entity, new FrameIndex());

            ref var animDataLink = ref dstManager.GetComponentData<AnimationSetLink>(entity).value.Value;
            var frameSize = new float2(initialAnim.MainTexSTOnAtlas.xy / initialAnim.GridSize);
            var framePosition = new int2(0 % initialAnim.GridSize.x, 0 / initialAnim.GridSize.x);
            _ = dstManager.AddComponentData(entity, new MainTexST { value = new float4(frameSize, initialAnim.MainTexSTOnAtlas.zw + frameSize * framePosition) });
        }

        private static void Dispose()
        {
            if (_blobAssetStore != null)
                _blobAssetStore?.Dispose();
        }
        static SpriteAnimationAuthoring() => Application.quitting -= () => Dispose();
    }
}