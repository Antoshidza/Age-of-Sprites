using Unity.Entities;

namespace NSprites
{
    /// <summary>
    /// Use this component during baking process to trigger it to remove default Unity.Transforms components
    /// </summary>
    [TemporaryBakingType]
    public struct RemoveDefaultTransformComponentsTag : IComponentData
    {
    }
}