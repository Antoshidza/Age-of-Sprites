using NSprites;
using Unity.Entities;

public struct AnimationDataLink : IComponentData
{
    public BlobAssetReference<SpriteAnimationBlobData> value;
}