using NSprites;
using Unity.Entities;

[assembly: InstancedPropertyComponent(typeof(SpriteSortingIndex), "_sortingIndexBuffer", PropertyFormat.Int)]
namespace NSprites
{
    public struct SpriteSortingIndex : IComponentData
    {
        public int value;
    }
}