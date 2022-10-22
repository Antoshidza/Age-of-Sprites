using Unity.Entities;
using Unity.Mathematics;

public struct MoveAround : IComponentData
{
    public float2 startPosition;
    public float2 area;
    public float timeOffset;
}
