using Unity.Entities;

namespace NSprites
{
    /// <summary>
    /// Attach this component to prevent entity baking 2D transform
    /// </summary>
    [TemporaryBakingType]
    public struct ExcludeFrom2DConversion : IComponentData { }
}
