using Unity.Entities;

namespace NSprites
{
    public struct LastParent2D : ICleanupComponentData
    {
        public Entity value;
    }
}