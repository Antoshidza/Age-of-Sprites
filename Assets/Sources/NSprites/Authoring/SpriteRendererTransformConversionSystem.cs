using Unity.Entities;
using Unity.Collections;

namespace NSprites
{
    [UpdateInGroup(typeof(GameObjectBeforeConversionGroup), OrderFirst = true)]
    [UpdateBefore(typeof(ExcludeTransformsConversionSystem))]
    public class SpriteRendererTransformConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            var ecb = new EntityCommandBuffer(Allocator.Temp);
            Entities
                .ForEach((Entity entity, SpriteRendererAuthoring spriteRendererAuthoring) =>
                {
                    if(spriteRendererAuthoring.ExcludeUnityTransformComponents)
                        ecb.AddComponent(entity, new ExcludeTransformFromConversion());
                    ecb.AddComponent(entity, new Transform2D() { gameObject = spriteRendererAuthoring.gameObject });
                });
            ecb.Playback(EntityManager);
        }
    }
}
