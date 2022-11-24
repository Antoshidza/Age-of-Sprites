using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites
{
    public class SpriteAnimationAuthoring : MonoBehaviour, IConvertGameObjectToEntity
    {
        [SerializeField] private SpriteAnimation _animationData;
        private static BlobAssetStore _blobAssetStore;

        private static BlobAssetStore BlobAssetStore => _blobAssetStore ??= new BlobAssetStore();

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            if (_animationData == null)
                return;

            #region create blob asset
            var blobBuilder = new BlobBuilder(Allocator.Temp); //can't use using because there is extension which use this + ref
            ref var root = ref blobBuilder.ConstructRoot<SpriteAnimationBlobData>();

            //fill data
            root.GridSize = _animationData.FrameCount;
            root.MainTexSTOnAtlas = NSpritesUtils.GetTextureST(_animationData.SpriteSheet);
            root.Scale2D = new float2(_animationData.SpriteSheet.bounds.size.x, _animationData.SpriteSheet.bounds.size.y);

            var animationDuration = 0f;
            for (int i = 0; i < _animationData.FrameDurations.Length; i++)
                animationDuration += _animationData.FrameDurations[i];

            root.AnimationDuration = animationDuration;

            var durations = blobBuilder.Allocate(ref root.FrameDurations, _animationData.FrameDurations.Length);
            for (int di = 0; di < durations.Length; di++)
                durations[di] = _animationData.FrameDurations[di];

            var blobAssetReference = blobBuilder.CreateBlobAssetReference<SpriteAnimationBlobData>(Allocator.Persistent);
            _ = BlobAssetStore.AddUniqueBlobAsset(ref blobAssetReference);
            blobBuilder.Dispose();
            #endregion

            _ = dstManager.AddComponentData(entity, new AnimationDataLink { value = blobAssetReference });
            _ = dstManager.AddComponentData(entity, new AnimationTimer { value = _animationData.FrameDurations[0] });
            _ = dstManager.AddComponentData(entity, new FrameIndex());

            ref var animData = ref dstManager.GetComponentData<AnimationDataLink>(entity).value.Value;
            var frameSize = new float2(animData.MainTexSTOnAtlas.xy / animData.GridSize);
            var framePosition = new int2(0 % animData.GridSize.x, 0 / animData.GridSize.x);
            _ = dstManager.AddComponentData(entity, new MainTexST { value = new float4(frameSize, animData.MainTexSTOnAtlas.zw + frameSize * framePosition) });
        }

        private static void Dispose()
        {
            if (_blobAssetStore != null)
                _blobAssetStore?.Dispose();
        }
        static SpriteAnimationAuthoring() => Application.quitting -= () => Dispose();
    }
}