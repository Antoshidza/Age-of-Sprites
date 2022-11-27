using Unity.Entities;
using Unity.Mathematics;

namespace NSprites
{
    [GenerateAuthoringComponent]
    public struct RandomColor : IComponentData
    {
        public Random rand;
        public float timer;
        public float randHue;
    }
}
