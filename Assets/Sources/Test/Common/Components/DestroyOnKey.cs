using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    [GenerateAuthoringComponent]
    public struct DestroyOnKey : IComponentData
    {
        public KeyCode value;
    }
}