using System.Collections.Generic;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace NSprites
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct SpriteSortingSystem : ISystem
    {
        #region data
        internal struct SortingData
        {
            internal struct GeneralComparer : IComparer<SortingData>
            {
                public int Compare(SortingData x, SortingData y)
                {
                    //can be rewrited with if statement
                    return x.groupSortingIndex.CompareTo(y.groupSortingIndex) * -32 //less index -> later in render
                        + x.groupPosition.CompareTo(y.groupPosition) * 16
                        + x.groupID.CompareTo(y.groupID) * 8
                        + x.sortingIndex.CompareTo(y.sortingIndex) * -4 //less index -> later in render
                        + x.position.y.CompareTo(y.position.y) * 2
                        + x.id.CompareTo(y.id);
                }
            }

            public int entityInChunkIndex;
            public int chunkIndex;

            public int id;
            public int groupID;

            public int sortingIndex;
            public int groupSortingIndex;

            public float2 position;
            public float groupPosition;
#if UNITY_EDITOR
            public override string ToString()
            {
                return $"id: {id}, groupID: {groupID}, groupIndex: {groupSortingIndex}, groupPos: {groupPosition}, sortIndex: {sortingIndex}, pos: {position}";
            }
#endif
        }
#endregion

        #region jobs
        [BurstCompile]
        internal struct GatherSortingDataJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<WorldPosition2D> worldPosition2D_CTH;
            [ReadOnly] public ComponentDataFromEntity<WorldPosition2D> worldPosition2D_CDFE;
            [ReadOnly] public ComponentTypeHandle<SortingGroup> sortingGroup_CTH;
            [ReadOnly] public ComponentDataFromEntity<SortingGroup> sortingGroup_CDFE;
            [WriteOnly][NativeDisableContainerSafetyRestriction] public NativeSlice<SortingData> sortingDataArray;

            public void Execute(ArchetypeChunk batchInChunk, int chunkIndex, int indexOfFirstEntityInQuery)
            {
                var entityArray = batchInChunk.GetNativeArray(entityTypeHandle);
                var worldPosition2DArray = batchInChunk.GetNativeArray(worldPosition2D_CTH);
                var sortingGroupArray = batchInChunk.GetNativeArray(sortingGroup_CTH);
                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var entity = entityArray[entityIndex];
                    var sortingGroup = sortingGroupArray[entityIndex];
                    var position = worldPosition2DArray[entityIndex].value;

                    float groupPosition;
                    int groupSortingIndex;
                    //means it's root entity
                    if (sortingGroup.groupID == entity)
                    {
                        groupPosition = position.y;
                        groupSortingIndex = sortingGroup.index;
                    }
                    else
                    {
                        groupPosition = worldPosition2D_CDFE[sortingGroup.groupID].value.y;
                        groupSortingIndex = sortingGroup_CDFE[sortingGroup.groupID].index;
                    }

                    sortingDataArray[indexOfFirstEntityInQuery + entityIndex] = new SortingData
                    {
                        entityInChunkIndex = entityIndex,
                        chunkIndex = chunkIndex,

                        position = position,

                        id = entity.Index,
                        sortingIndex = sortingGroup.index,

                        groupID = sortingGroup.groupID.Index,
                        groupPosition = groupPosition,
                        groupSortingIndex = groupSortingIndex,
                    };
                }
            }
        }
        [BurstCompile]
        internal struct SortArrayJob<TElement, TComparer> : IJob
            where TElement : unmanaged
            where TComparer : unmanaged, IComparer<TElement>
        {
            public NativeArray<TElement> array;
            public TComparer comparer;

            public void Execute() => array.Sort(comparer);
        }
        [BurstCompile]
        internal struct DistributeSortingDataToChunksJob : IJobParallelForBatch
        {
            [WriteOnly] public NativeParallelMultiHashMap<int, int2>.ParallelWriter sortingDataToChunkHashMap;
            [ReadOnly] public NativeArray<SortingData> sortingDataArray;

            public void Execute(int startIndex, int count)
            {
                var toIndex = startIndex + count;
                for (int i = startIndex; i < toIndex; i++)
                {
                    var sortingDataElement = sortingDataArray[i];
                    sortingDataToChunkHashMap.Add(sortingDataElement.chunkIndex, new int2(i, sortingDataElement.entityInChunkIndex));
                }
            }
        }
        [BurstCompile]
        internal struct WriteBackIndexesJob : IJobChunk
        {
            public ComponentTypeHandle<SpriteSortingIndex> spriteSortingIndex_CTH;
            [ReadOnly] public NativeParallelMultiHashMap<int, int2> sortingDataToChunkHashMap;

            public void Execute(ArchetypeChunk chunk, int chunkIndex, int firstEntityIndex)
            {
                var sortingIndexes = chunk.GetNativeArray(spriteSortingIndex_CTH);

                if (sortingDataToChunkHashMap.TryGetFirstValue(chunkIndex, out var writeBackElement, out var iterator))
                    do
                        sortingIndexes[writeBackElement.y] = new SpriteSortingIndex { value = writeBackElement.x };
                    while (sortingDataToChunkHashMap.TryGetNextValue(out writeBackElement, ref iterator));
            }
        }
        #endregion

        private EntityQuery _sortingSpritesQuery;

        public void OnCreate(ref SystemState state)
        {
            _sortingSpritesQuery = state.GetEntityQuery
            (
                ComponentType.ReadOnly<SpriteRendererTag>(),
                ComponentType.ReadOnly<SpriteRenderID>(),

                ComponentType.ReadOnly<WorldPosition2D>(),
                ComponentType.ReadOnly<SortingGroup>(),

                ComponentType.ReadOnly<SpriteSortingIndex>()
            );
        }
        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            //TODO: do it for different layers

            var spriteEntitiesCount = _sortingSpritesQuery.CalculateEntityCount();
            var sortingDataArray = new NativeArray<SortingData>(spriteEntitiesCount, Allocator.TempJob);

            var gatherSortingDataJob = new GatherSortingDataJob
            {
                entityTypeHandle = state.GetEntityTypeHandle(),
                worldPosition2D_CTH = state.GetComponentTypeHandle<WorldPosition2D>(true),
                worldPosition2D_CDFE = state.GetComponentDataFromEntity<WorldPosition2D>(true),
                sortingGroup_CTH = state.GetComponentTypeHandle<SortingGroup>(true),
                sortingGroup_CDFE = state.GetComponentDataFromEntity<SortingGroup>(true),
                sortingDataArray = sortingDataArray
            };
            var gatherSortingDataHandle = gatherSortingDataJob.ScheduleParallelByRef(_sortingSpritesQuery, state.Dependency);

            // TODO: sort simple array of indexes instead, because SortingData is to large to copy
            // TODO: optimize sorting by splitting screen on N squares, where N is number of threads
            var sortHandle = new SortArrayJob<SortingData, SortingData.GeneralComparer>
            {
                array = sortingDataArray,
                comparer = new SortingData.GeneralComparer()
            }.Schedule(gatherSortingDataHandle);

            var sortingDataToChunkHashMap = new NativeParallelMultiHashMap<int, int2>(spriteEntitiesCount, Allocator.TempJob);

            var distributeSortingDataToChunksHandle = new DistributeSortingDataToChunksJob()
            {
                sortingDataArray = sortingDataArray,
                sortingDataToChunkHashMap = sortingDataToChunkHashMap.AsParallelWriter()
            }.ScheduleBatch(spriteEntitiesCount, 64,sortHandle);

            _ = sortingDataArray.Dispose(distributeSortingDataToChunksHandle);

            var writeBackIndexesJob = new WriteBackIndexesJob
            {
                sortingDataToChunkHashMap = sortingDataToChunkHashMap,
                spriteSortingIndex_CTH = state.GetComponentTypeHandle<SpriteSortingIndex>(false)
            };
            var writeBackIndexesHandle = writeBackIndexesJob.ScheduleParallelByRef(_sortingSpritesQuery, distributeSortingDataToChunksHandle);

            _ = sortingDataToChunkHashMap.Dispose(writeBackIndexesHandle);

            state.Dependency = writeBackIndexesHandle;
        }
    }
}