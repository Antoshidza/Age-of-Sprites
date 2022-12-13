using Unity.Entities;
using Unity.Burst;
using Unity.Mathematics;

namespace NSprites
{
    public class SpriteRendererAuthoring : BaseSpriteRendererAuthoring
    {
        [BurstCompile]
        [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
        private partial struct SpriteRendererBakingSystem : ISystem
        {
            public void OnCreate(ref SystemState state) {}
            public void OnDestroy(ref SystemState state) {}
            public void OnUpdate(ref SystemState state)
            {
                // add sprite render components to each entity which will be registered as sprite
                var query = state.GetEntityQuery
                (
                    new EntityQueryDesc
                    {
                        All = new ComponentType[] { ComponentType.ReadOnly<SpriteRenderDataToRegistrate>() },
                        Options = EntityQueryOptions.IncludePrefab
                    }
                );
                state.EntityManager.AddSpriteRenderComponents(query);
            }
        }
        private class SpriteRendererBaker : Baker<SpriteRendererAuthoring>
        {
            public override void Bake(SpriteRendererAuthoring authoring)
                => AddComponent(new MainTexST { value = NSpritesUtils.GetTextureST(authoring._sprite) });
        }
    }
}
