using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    /// <summary>
    /// Attach this component to gameobject to get rid of any default Unity.Transforms components after conversion
    /// </summary>
    public class RemoveDefaultTransformComponentsAuthoring : MonoBehaviour
    {
        private class RemoveDefaultTransformComponentsBaker : Baker<RemoveDefaultTransformComponentsAuthoring>
        {
            public override void Bake(RemoveDefaultTransformComponentsAuthoring authoring)
            {
                AddComponent<RemoveDefaultTransformComponentsTag>();
            }
        }
    }
}