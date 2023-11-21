using Unity.Entities;
using Unity.Mathematics;

namespace NSprites
{
    public struct WorldPosition2D : IComponentData
    {
        public float2 Value;

        public WorldPosition2D(in float3 pos) => Value = pos.xy;
    }
}