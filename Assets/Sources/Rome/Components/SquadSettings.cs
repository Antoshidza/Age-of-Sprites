using System;
using Unity.Entities;
using Unity.Mathematics;

public struct SquadSettings : IComponentData, IEquatable<SquadSettings>
{
    public Entity soldierPrefab;
    public float2 soldierSize;
    public float2 soldierMargin;
    public int2 squadResolution;

    public bool Equals(SquadSettings other)
    {
        return math.all(soldierMargin == other.soldierMargin)
            && math.all(squadResolution == other.squadResolution);
    }
    public static bool operator ==(SquadSettings a, SquadSettings b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(SquadSettings a, SquadSettings b)
    {
        return !a.Equals(b);
    }
}
