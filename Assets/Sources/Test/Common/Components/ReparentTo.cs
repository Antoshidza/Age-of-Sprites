using Unity.Entities;

namespace NSprites
{
    [GenerateAuthoringComponent]
    public struct ReparentTo : IComponentData
    {
        public Entity entity;
    }
}