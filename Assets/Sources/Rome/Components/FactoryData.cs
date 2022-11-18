using Unity.Entities;
using Unity.Mathematics;

public struct FactoryData : IComponentData
{
    public Entity prefab;
    public int count;
    public float duration;
    public float2 instantiatePos;
}