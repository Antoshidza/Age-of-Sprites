using System.Collections.Generic;
using UnityEngine;

namespace NSprites
{
    public class MaterialInspector : MonoBehaviour
    {
        [SerializeField]
        private Material _material;
        [SerializeField]
        private Sprite _overrideSprite;

        private void Start()
        {
            var str = $"initial mat CRC: {_material.ComputeCRC()}\ncopy mat CRC: {new Material(_material).ComputeCRC()}";
            var overrideMaterial = new Material(_material);
            str += $"\nmaterial == overrideMaterial (before value change): {_material == overrideMaterial}";
            str += $"\nmaterial equalTo overrideMaterial (before value change): {_material.Equals(overrideMaterial)}";
            overrideMaterial.SetTexture("_MainTex", _overrideSprite.texture);
            str += $"\noverride mat CRC: {overrideMaterial.ComputeCRC()}";
            var fakeOverrideMaterial = new Material(_material);
            str += $"\nfake override mat HASH: {fakeOverrideMaterial.GetHashCode()}";
            fakeOverrideMaterial.SetFloat("_BumpScale", 404f);
            str += $", next HASH: {fakeOverrideMaterial.GetHashCode()}";
            str += $"\nfake override mat CRC: {fakeOverrideMaterial.ComputeCRC()}";
            Debug.Log(str);
        }
    }
}
