﻿using Unity.Entities;
using Unity.Mathematics;

[GenerateAuthoringComponent]
public struct FrameGrid : IComponentData
{
    public int2 size;
}
