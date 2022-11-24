using Unity.Entities;
using Unity.Mathematics;

public struct SpawnerData : IComponentData
{
    public int count;
    public int totalCount;
    public int countPerSpawn;
    public int spawnAcceleration;
    public float2x2 spawnBounds;
}