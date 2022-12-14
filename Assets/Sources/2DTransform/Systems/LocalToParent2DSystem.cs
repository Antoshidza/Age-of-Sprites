using Unity.Entities;
using Unity.Transforms;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Mathematics;
using Unity.Burst;
using Unity.Burst.Intrinsics;

namespace NSprites
{
    [UpdateInGroup(typeof(TransformSystemGroup))]
    public partial class LocalToParent2DSystem : SystemBase
    {
        [BurstCompile]
        private struct UpdateHierarchy : IJobChunk
        {
            [ReadOnly] public ComponentTypeHandle<WorldPosition2D> worldPosition_CTH;
            [NativeDisableContainerSafetyRestriction] public ComponentLookup<WorldPosition2D> worldPosition_CDFE;
            [ReadOnly] public ComponentLookup<LocalPosition2D> localPosition_CDFE;
            [ReadOnly] public BufferTypeHandle<Child2D> child_BTH;
            [ReadOnly] public BufferLookup<Child2D> child_BFE;
            public uint lastSystemVersion;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                //if position or child set was changed then we need update children hierarchically
                var needUpdate = chunk.DidChange(ref worldPosition_CTH, lastSystemVersion) && chunk.DidChange(ref child_BTH, lastSystemVersion);

                var chunkWorldPosition = chunk.GetNativeArray(ref worldPosition_CTH);
                var chunkChild = chunk.GetBufferAccessor(ref child_BTH);

                for (int entityIndex = 0; entityIndex < chunk.Count; entityIndex++)
                {
                    var worldPosition = chunkWorldPosition[entityIndex];
                    var children = chunkChild[entityIndex];

                    for (int childIndex = 0; childIndex < children.Length; childIndex++)
                        UpdateChild(worldPosition.value, children[childIndex].value, needUpdate);
                }
            }

            private void UpdateChild(in float2 parentPosition, in Entity childEntity, bool needUpdate)
            {
                var position = parentPosition + localPosition_CDFE[childEntity].value;
                worldPosition_CDFE[childEntity] = new WorldPosition2D { value = position };

                //if this child also is a parent update its children
                if (!child_BFE.HasBuffer(childEntity))
                    return;

                needUpdate = needUpdate || localPosition_CDFE.DidChange(childEntity, lastSystemVersion) || child_BFE.DidChange(childEntity, lastSystemVersion);
                var children = child_BFE[childEntity];

                for (int childIndex = 0; childIndex < children.Length; childIndex++)
                    UpdateChild(position, children[childIndex].value, needUpdate);
            }
        }

        private EntityQuery _rootQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _rootQuery = GetEntityQuery
            (
                new EntityQueryDesc
                {
                    All = new ComponentType[]
                    {
                        typeof(WorldPosition2D),
                        typeof(Child2D)
                    },
                    None = new ComponentType[]
                    {
                        typeof(Parent2D)
                    }
                }
            );
        }
        protected override void OnUpdate()
        {
            Dependency = new UpdateHierarchy
            {
                worldPosition_CDFE = GetComponentLookup<WorldPosition2D>(false),
                localPosition_CDFE = GetComponentLookup<LocalPosition2D>(true),
                child_BFE = GetBufferLookup<Child2D>(true),
                child_BTH = GetBufferTypeHandle<Child2D>(true),
                worldPosition_CTH = GetComponentTypeHandle<WorldPosition2D>(true),
                lastSystemVersion = LastSystemVersion
            }.ScheduleParallel(_rootQuery, Dependency);
        }
    }
}
