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

        [SerializeField] private SpriteAnimation fixNullRefSO_1; // TODO: remove it after fixing https://github.com/Antoshidza/Age-of-Sprites/issues/6
    }
}
