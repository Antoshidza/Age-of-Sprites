using UnityEngine;

namespace NSprites
{
    [CreateAssetMenu(fileName = "PropertiesSet", menuName = "Shader Properties Set", order = 1)]
    public class PropertiesSet : ScriptableObject
    {
        /// <summary>Holds info about: what shader's property name / update mode property uses</summary>
        [System.Serializable]
        private struct PropertyDataRaw
        {
            public string propertyName;
            public PropertyUpdateMode updateMode;

            public static implicit operator PropertyData(PropertyDataRaw rawData)
                => new(Shader.PropertyToID(rawData.propertyName), rawData.updateMode);
            internal static PropertyData[] BakeData(PropertyDataRaw[] rawDataSet)
            {
                if (rawDataSet == null)
                    return null;
                var result = new PropertyData[rawDataSet.Length];
                for (int i = 0; i < rawDataSet.Length; i++)
                    result[i] = rawDataSet[i];
                return result;
            }
        }

        [SerializeField]
        private PropertyDataRaw[] propertiesRawData;

        public PropertyData[] PropertyData => PropertyDataRaw.BakeData(propertiesRawData);
    }
}
