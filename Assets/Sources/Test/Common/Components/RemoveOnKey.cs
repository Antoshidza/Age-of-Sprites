using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    [GenerateAuthoringComponent]
    public struct RemoveOnKey : IComponentData
    {
        public KeyCode keyCode;
    }
}