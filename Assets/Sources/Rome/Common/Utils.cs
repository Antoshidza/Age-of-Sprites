using System.Runtime.CompilerServices;
using Unity.Collections;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

public static class Utils
{
#if UNITY_EDITOR
    public enum DrawType
    {
        Gizmo,
        Handles,
        Debug
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawRect(in float2x2 pos, in Color color, in float z = 0f, in DrawType drawType = DrawType.Gizmo)
    {
        var positions = new NativeArray<float3>(4, Allocator.Temp);
        positions[0] = pos.c0.ToFloat3(z);
        positions[1] = new float3(pos.c0.x, pos.c1.y, z);
        positions[2] = new float3(pos.c1.x, pos.c1.y, z);
        positions[3] = new float3(pos.c1.x, pos.c0.y, z);

        DrawLines(positions, color, true, drawType);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLines(in NativeArray<float3> positions, in Color color, in bool isClosing = true, in DrawType drawType = DrawType.Gizmo)
    {
        if (positions.Length < 2)
            return;

        var prevPos = positions[0];
        for (int i = 1; i < positions.Length; i++)
        {
            var pos = positions[i];
            DrawLine(prevPos, pos, color, drawType);
            prevPos = pos;
        }

        if(isClosing)
            DrawLine(prevPos, positions[0], color, drawType);
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void DrawLine(in float3 a, in float3 b, in Color color, in DrawType drawType = DrawType.Gizmo)
    {
        Color tmpColor;

        if (drawType == DrawType.Gizmo)
        {
            tmpColor = Gizmos.color;
            Gizmos.color = color;
            Gizmos.DrawLine(a, b);
            Gizmos.color = tmpColor;
        }
        else if (drawType == DrawType.Handles)
        {
            tmpColor = Handles.color;
            Handles.color = color;
            Handles.DrawLine(a, b);
            Handles.color = tmpColor;
        }
        else if (drawType == DrawType.Debug)
            Debug.DrawLine(a, b, color);
    }
#endif
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float3 ToFloat3(this in float2 value, in float z = 0f)
    {
        return new float3(value.x, value.y, z);
    }
}