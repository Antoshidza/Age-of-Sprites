using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    /// <summary>
    /// Attach this component during baking process to trigger 2D transform component baking such as
    /// <para><see cref="WorldPosition2D"/>, 
    /// <see cref="LocalPosition2D"/>, 
    /// <see cref="Parent2D"/>, 
    /// <see cref="LastParent2D"/>, 
    /// <see cref="Child2D"/>, 
    /// <see cref="StaticRelationshipsTag"/></para>
    /// </summary>
    [TemporaryBakingType]
    public class Transform2DRequest : IComponentData
    {
        public GameObject sourceGameObject;
    }
}