using Unity.Entities;

namespace NSprites
{
    public struct AnimationSettings : IComponentData
    {
        public int IdleHash;
        public int WalkHash;
    }
}