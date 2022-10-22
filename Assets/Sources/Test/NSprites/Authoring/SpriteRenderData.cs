using System;
using UnityEngine;

namespace NSprites
{
    [Serializable]
    public struct SpriteRenderData
    {
        public int ID => Material.GetHashCode();
        public Material Material;
        public PropertiesSet PropertiesSet;
    }
}
