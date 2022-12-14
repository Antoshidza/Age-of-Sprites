using Unity.Entities;
using Unity.Transforms;

namespace NSprites
{
    [WorldSystemFilter(WorldSystemFilterFlags.BakingSystem)]
    public partial struct RemoveDefaultTransformComponentsSystem : ISystem
    {
        private struct RemoveComponent<T>
            where T : IComponentData
        {
            private EntityQuery query;

            public void Initialize(ref SystemState state)
                => query = state.GetEntityQuery(new EntityQueryDesc() { All = new ComponentType[] { typeof(T) }, Options = EntityQueryOptions.IncludePrefab });
            public void Perform(ref SystemState state)
                => state.EntityManager.RemoveComponent<T>(query);
        }

        private RemoveComponent<LocalToWorld> _localToWorld_RC;
        private RemoveComponent<PreviousParent> _previousParent_RC;
        private RemoveComponent<LocalTransform> _localTransform_RC;
        private RemoveComponent<ParentTransform> _parentTransform_RC;
        private RemoveComponent<PostTransformScale> _postTransformScale_RC;
        private RemoveComponent<PropagateLocalToWorld> _propagateLocalToWorld_RC;
        private RemoveComponent<WorldTransform> _worldTransform_RC;

        public void OnCreate(ref SystemState state)
        {
            _localToWorld_RC.Initialize(ref state);
            _previousParent_RC.Initialize(ref state);
            _localTransform_RC.Initialize(ref state);
            _parentTransform_RC.Initialize(ref state);
            _postTransformScale_RC.Initialize(ref state);
            _propagateLocalToWorld_RC.Initialize(ref state);
            _worldTransform_RC.Initialize(ref state);
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            _localToWorld_RC.Perform(ref state);
            _previousParent_RC.Perform(ref state);
            _localTransform_RC.Perform(ref state);
            _parentTransform_RC.Perform(ref state);
            _postTransformScale_RC.Perform(ref state);
            _propagateLocalToWorld_RC.Perform(ref state);
            _worldTransform_RC.Perform(ref state);
        }
    }
}