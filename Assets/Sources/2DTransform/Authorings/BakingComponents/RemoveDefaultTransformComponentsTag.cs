using Unity.Entities;

namespace NSprites
{
    /// <summary>
    /// Use this component during baking process to trigger it to remove default Unity.Transforms components
    /// </summary>
    [BakingType]
    public struct RemoveDefaultTransformComponentsTag : IComponentData
    {
    }
}