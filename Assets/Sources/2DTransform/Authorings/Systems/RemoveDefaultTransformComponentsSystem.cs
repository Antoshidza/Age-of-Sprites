using Unity.Entities;
using Unity.Transforms;

namespace NSprites
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct RemoveDefaultTransformComponentsSystem : ISystem
    {
        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            void RemoveAllComponentInstances<T>(ref SystemState state)
                where T : IComponentData
                => state.EntityManager.RemoveComponent<T>(state.GetEntityQuery(new EntityQueryDesc { All = new ComponentType[] { typeof(T) }, Options = EntityQueryOptions.IncludePrefab }));

            RemoveAllComponentInstances<LocalToWorld>(ref state);
            RemoveAllComponentInstances<PreviousParent>(ref state);
            //RemoveAllComponentInstances<LocalTransform>(ref state);
            //RemoveAllComponentInstances<ParentTransform>(ref state);
            //RemoveAllComponentInstances<PostTransformScale>(ref state);
            //RemoveAllComponentInstances<PropagateLocalToWorld>(ref state);
            //RemoveAllComponentInstances<WorldTransform>(ref state);
        }
    }
}