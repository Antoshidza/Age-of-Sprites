using UnityEngine;

namespace NSprites
{
    [CreateAssetMenu(fileName = "PropertiesSet", menuName = "Shader Properties Set", order = 1)]
    public class PropertiesSet : ScriptableObject
    {
        [SerializeField]
        public string[] PropertiesNames;
    }
}
