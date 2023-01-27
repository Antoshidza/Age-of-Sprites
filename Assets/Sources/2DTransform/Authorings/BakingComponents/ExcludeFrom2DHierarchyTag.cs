using Unity.Entities;

namespace NSprites
{
    /// <summary>
    /// Attach this component to prevent entity persist in 2D hierarchy (though 2D transform baking included)
    /// </summary>
    [TemporaryBakingType]
    public struct ExcludeFrom2DHierarchyTag : IComponentData { }
}
