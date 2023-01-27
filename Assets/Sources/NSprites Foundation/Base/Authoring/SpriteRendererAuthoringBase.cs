using Unity.Burst;
using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    /// <summary>
    /// Gets <see cref="SpriteRenderData"/> through virtual <see cref="RenderData"/> property then adds <see cref="SpriteRenderDataToRegister"/>.
    /// Lately <see cref="SpriteRenderBakingSystem"/> will catch those entities and add needed components for rendering. 
    /// </summary>
    public abstract class SpriteRendererAuthoringBase : MonoBehaviour
    {
        [BurstCompile]
        [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
        private partial struct SpriteRenderBakingSystem : ISystem
        {
            private EntityQuery _query;

            public void OnCreate(ref SystemState state)
            {
                _query = state.GetEntityQuery
                (
                    new EntityQueryDesc
                    {
                        All = new [] { ComponentType.ReadOnly<SpriteBakeRequest>() },
                        Options = EntityQueryOptions.IncludePrefab
                    }
                );
            }
            public void OnDestroy(ref SystemState state) {}
            public void OnUpdate(ref SystemState state)
            {
                // add sprite render components to each entity which will be registered as sprite
                state.EntityManager.AddSpriteRenderComponents(_query);
            }
        }
        
        [BakeDerivedTypes]
        private class SpriteRendererBaker : Baker<SpriteRendererAuthoringBase>
        {
            public override void Bake(SpriteRendererAuthoringBase authoring)
            {
                var renderData = authoring.RenderData;
                
                DependsOn(renderData.PropertiesSet);
                AddComponentObject(new SpriteRenderDataToRegister { data = renderData });
                AddComponent<SpriteBakeRequest>(); // to trigger baking system
            }
        }

        protected abstract SpriteRenderData RenderData { get; }
    }
}