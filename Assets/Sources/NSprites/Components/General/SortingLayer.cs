using System;
using Unity.Entities;

public struct SortingLayer : ISharedComponentData, IComparable<SortingLayer>
{
    public int index;

    public int CompareTo(SortingLayer other)
    {
        return index.CompareTo(other.index);
    }
}