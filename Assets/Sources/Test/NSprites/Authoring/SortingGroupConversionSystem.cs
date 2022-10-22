using Unity.Entities;

namespace NSprites
{
    [UpdateInGroup(typeof(GameObjectAfterConversionGroup))]
    public class SortingGroupConversionSystem : GameObjectConversionSystem
    {
        protected override void OnUpdate()
        {
            Entities
                .ForEach((SpriteRendererAuthoring spriteRendererAuthoring) =>
                {
                    var entity = GetPrimaryEntity(spriteRendererAuthoring);
                    DstEntityManager.AddComponentData
                    (
                        entity,
                        new SortingGroup()
                        {
                            index = spriteRendererAuthoring.SortingIndex,
                            groupID = DstEntityManager.HasComponent<Parent2D>(entity) ?
                                DstEntityManager.GetComponentData<Parent2D>(entity).value :
                                entity
                        }
                    );
                });
        }
    }
}
