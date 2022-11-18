using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct MapSettings : IComponentData
{
    public float2x2 size;
    public Entity rockCollectionLink;
    public int rockCount;
}