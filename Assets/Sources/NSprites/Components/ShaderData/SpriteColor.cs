using NSprites;
using Unity.Entities;
using UnityEngine;

[assembly: InstancedPropertyComponent(typeof(SpriteColor), "_colorBuffer", PropertyFormat.Float4)]
namespace NSprites
{
    [GenerateAuthoringComponent]
    public struct SpriteColor : IComponentData
    {
        public Color color;
    }
}
