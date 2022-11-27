using Unity.Entities;

[GenerateAuthoringComponent]
public struct FrameIndex : IComponentData
{
    public int value;
}
