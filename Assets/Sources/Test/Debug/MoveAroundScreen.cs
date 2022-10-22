using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct MoveAroundScreen : IComponentData
{
    public float2 destination;
}