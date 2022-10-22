using NSprites;
using Unity.Entities;
using Unity.Mathematics;

[assembly: InstancedPropertyComponent(typeof(MainTexST), "_mainTexSTBuffer", PropertyFormat.Float4)]
namespace NSprites
{
    public struct MainTexST : IComponentData
    {
        public float4 value;
    }
}
