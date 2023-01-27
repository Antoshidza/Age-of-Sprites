using System;
using UnityEngine;

namespace NSprites
{
    [CreateAssetMenu(fileName = "PropertiesSet", menuName = "NSprites/Shader Properties Set", order = 1)]
    public class PropertiesSet : ScriptableObject
    {
        /// <summary>Holds info about: what shader's property name / update mode property uses</summary>
        [Serializable]
        private struct PropertyDataRaw
        {
            public string propertyName;
            public PropertyUpdateMode updateMode;

            public static implicit operator PropertyData(PropertyDataRaw rawData)
                => new(rawData.propertyName, rawData.updateMode);
            internal static PropertyData[] BakeData(PropertyDataRaw[] rawDataSet)
            {
                if (rawDataSet == null)
                    return default;
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
