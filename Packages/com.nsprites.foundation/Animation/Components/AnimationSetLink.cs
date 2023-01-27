using NSprites;
using Unity.Entities;

public struct AnimationSetLink : IComponentData
{
    public BlobAssetReference<BlobArray<SpriteAnimationBlobData>> value;
}