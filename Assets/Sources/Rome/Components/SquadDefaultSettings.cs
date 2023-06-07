using System;
using System.Runtime.CompilerServices;
using Unity.Entities;
using Unity.Mathematics;

public struct SquadDefaultSettings : IComponentData, IEquatable<SquadDefaultSettings>
{
    public Entity soldierPrefab;
    public SquadSettings defaultSettings;

    public float2 SoldierMargin => defaultSettings.soldierMargin;
    public int2 SquadResolution => defaultSettings.squadResolution;

    public int SoldierCount => SquadResolution.x * SquadResolution.y;

    public bool Equals(SquadDefaultSettings other)
    {
        return defaultSettings == other.defaultSettings;
    }
    public static bool operator ==(SquadDefaultSettings a, SquadDefaultSettings b)
    {
        return a.Equals(b);
    }
    public static bool operator !=(SquadDefaultSettings a, SquadDefaultSettings b)
    {
        return !a.Equals(b);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float2 GetSquadSize(in int2 squadResolution, in float2 soldierSize, in float2 soldierMargine)
    {
        return squadResolution * (2 * soldierMargine + 1f) * soldierSize;
    }

    public override bool Equals(object obj) => base.Equals(obj);
    public override int GetHashCode() => base.GetHashCode();
}
