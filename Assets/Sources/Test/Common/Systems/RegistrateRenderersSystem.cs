using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;

namespace NSprites
{
    [WorldSystemFilter(WorldSystemFilterFlags.Editor | WorldSystemFilterFlags.Default)]
    public partial class RegistrateRenderersSystem : SystemBase
    {
        private SpriteRenderingSystem _spriteRenderingSystem;
        private EntityQuery _renderArchetypeToRegistrateQuery;
        private EntityQuery _renderArchetypeIndexLessEntitiesQuery;
        private HashSet<int> _registeredIDsSet = new();

        protected override void OnCreate()
        {
            base.OnCreate();
            _spriteRenderingSystem = World.GetOrCreateSystem<SpriteRenderingSystem>();
            _renderArchetypeToRegistrateQuery = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<SpriteRenderDataToRegistrate>(),
                        ComponentType.ReadOnly<SpriteRenderID>()
                    },
                    Options = EntityQueryOptions.IncludePrefab
                }
            );
            _renderArchetypeIndexLessEntitiesQuery = GetEntityQuery
            (
                 new EntityQueryDesc
                 {
                    All = new ComponentType[]
                    {
                        ComponentType.ReadOnly<SpriteRenderDataToRegistrate>()
                    },
                    None = new ComponentType[]
                    {
                        ComponentType.ReadOnly<SpriteRenderID>()
                    },
                    Options = EntityQueryOptions.IncludePrefab
                 }
            );
    }
        protected override void OnUpdate()
        {
            EntityManager.AddComponent<SpriteRenderID>(_renderArchetypeIndexLessEntitiesQuery);

            void Registrate(in NativeArray<Entity> entities)
            {
                for(int i = 0; i < entities.Length; i++)
                {
                    var entity = entities[i];
                    var renderData = EntityManager.GetComponentObject<SpriteRenderDataToRegistrate>(entity);

                    if (!_registeredIDsSet.Contains(renderData.data.ID))
                    {
                        _spriteRenderingSystem.RegistrateRender
                        (
                            renderData.data.ID,
                            renderData.data.Material,
                            renderData.data.PropertiesSet.PropertiesNames,
                            capacityStep: 100
                        );
                        _registeredIDsSet.Add(renderData.data.ID);
                    }

                    EntityManager.SetSharedComponentData
                    (
                        entity,
                        new SpriteRenderID{ id = renderData.data.ID }
                    );
                }
            }
            Registrate(_renderArchetypeToRegistrateQuery.ToEntityArray(Allocator.Temp));

            EntityManager.RemoveComponent<SpriteRenderDataToRegistrate>(_renderArchetypeToRegistrateQuery);
        }
    }
}
