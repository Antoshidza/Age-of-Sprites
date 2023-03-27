using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    public class AnimationSettingsAuthoring : MonoBehaviour
    {
        private class AnimationSettingsBaker : Baker<AnimationSettingsAuthoring>
        {
            public override void Bake(AnimationSettingsAuthoring authoring)
            {
                AddComponent(GetEntity(TransformUsageFlags.None), new AnimationSettings
                {
                    IdleHash = Animator.StringToHash("idle"),
                    WalkHash = Animator.StringToHash("walk")
                });
            }
        }
    }
}