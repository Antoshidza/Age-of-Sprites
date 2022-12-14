using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace NSprites
{
    // TODO: try to get rid of managed things in 1.0 entities and make system burstable ISystem
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial class SpriteSortingSystem : SystemBase
    {
        #region data
        internal struct SortingData
        {
            internal struct GeneralComparer : IComparer<SortingData>
            {
                public int Compare(SortingData x, SortingData y)
                {
                    return x.sortingIndex.CompareTo(y.sortingIndex) * -4 //less index -> later in render
                        + x.position.y.CompareTo(y.position.y) * 2
                        + x.id.CompareTo(y.id);
                }
            }

            public int id;
            public int sortingIndex;
            public float2 position;
#if UNITY_EDITOR
            public override string ToString()
            {
                return $"id: {id}, sortIndex: {sortingIndex}, pos: {position}";
            }
#endif
        }
        internal struct SortingDataComparer : IComparer<int>
        {
            public NativeArray<SortingData> sourceData;
            public SortingData.GeneralComparer sourceDataComparer;

            public int Compare(int x, int y) => sourceDataComparer.Compare(sourceData[x], sourceData[y]);
        }
        #endregion

        #region jobs
        [BurstCompile]
        internal struct GatherSortingDataJob : IJobChunk
        {
            [ReadOnly] public EntityTypeHandle entityTypeHandle;
            [ReadOnly] public ComponentTypeHandle<WorldPosition2D> worldPosition2D_CTH;
            [ReadOnly] public ComponentLookup<WorldPosition2D> worldPosition2D_CDFE;
            [ReadOnly] public ComponentTypeHandle<SortingIndex> sortingIndex_CTH;
            [WriteOnly][NativeDisableContainerSafetyRestriction] public NativeArray<SortingData> sortingDataArray;
            [WriteOnly][NativeDisableContainerSafetyRestriction] public NativeArray<int> pointers;
            [ReadOnly] public NativeArray<int> chunkBasedEntityIndeces;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var entityArray = chunk.GetNativeArray(entityTypeHandle);
                var worldPosition2DArray = chunk.GetNativeArray(ref worldPosition2D_CTH);
                var sortingIndexes = chunk.GetNativeArray(ref sortingIndex_CTH);
                var firstEntityIndex = chunkBasedEntityIndeces[unfilteredChunkIndex];
                for (int entityIndex = 0; entityIndex < entityArray.Length; entityIndex++)
                {
                    var arrayIndex = firstEntityIndex + entityIndex;
                    sortingDataArray[arrayIndex] = new SortingData
                    {
                        position = worldPosition2DArray[entityIndex].value,
                        sortingIndex = sortingIndexes[entityIndex].value,
                        id = entityArray[entityIndex].Index
                    };
                    pointers[arrayIndex] = arrayIndex;
                }
            }
        }
        [BurstCompile]
        internal struct SortArrayJob<TElement, TComparer> : IJob
            where TElement : unmanaged
            where TComparer : struct, IComparer<TElement>
        {
            public NativeArray<TElement> array;
            public TComparer comparer;

            public void Execute() => array.Sort(comparer);
        }
        [BurstCompile]
        internal struct GenerateSortingValuesJob : IJobParallelForBatch
        {
            [ReadOnly] public NativeArray<int> pointers;
            [WriteOnly][NativeDisableParallelForRestriction] public NativeArray<SortingValue> sortingValues;
            public int layerIndex;

            public void Execute(int startIndex, int count)
            {
                var toIndex = startIndex + count;
                for (int i = startIndex; i < toIndex; i++)
                {
                    var from = PerLayerOffset * layerIndex;
                    var to = from + PerLayerOffset;
                    sortingValues[pointers[i]] = new SortingValue { value = math.lerp(from, to, 1f - (float)i / pointers.Length) };
                }
            }
        }
        [BurstCompile]
        internal struct WriteSortingValuesToChunksJob : IJobChunk
        {
            [ReadOnly] public NativeArray<int> chunkBasedEntityIndeces;
            [NativeDisableContainerSafetyRestriction] public ComponentTypeHandle<SortingValue> sortingValue_CTH_WO;
            [ReadOnly] public NativeArray<SortingValue> sortingValues;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                var chunkSortingValues = chunk.GetNativeArray(ref sortingValue_CTH_WO);
                NativeArray<SortingValue>.Copy(sortingValues, chunkBasedEntityIndeces[unfilteredChunkIndex], chunkSortingValues, 0, chunkSortingValues.Length);
            }
        }
        #endregion

        private const int LayerCount = 8;
        private const float PerLayerOffset = 1f / LayerCount;
        private EntityQuery _sortingSpritesQuery;
        private EntityQuery _sortingStaticSpritesQuery;
        private List<SortingLayer> _sortingLayers;

        protected override void OnCreate()
        {
            base.OnCreate();
            _sortingSpritesQuery = GetEntityQuery
            (
                ComponentType.Exclude<CullSpriteTag>(),

                ComponentType.ReadOnly<WorldPosition2D>(),

                ComponentType.ReadOnly<SortingValue>(),
                ComponentType.ReadOnly<SortingIndex>(),
                ComponentType.ReadOnly<SortingLayer>(),
                ComponentType.ReadOnly<VisualSortingTag>(),
                ComponentType.Exclude<SortingStaticTag>()
            );
            _sortingStaticSpritesQuery = GetEntityQuery
            (
                ComponentType.Exclude<CullSpriteTag>(),

                ComponentType.ReadOnly<WorldPosition2D>(),

                ComponentType.ReadOnly<SortingValue>(),
                ComponentType.ReadOnly<SortingIndex>(),
                ComponentType.ReadOnly<SortingLayer>(),
                ComponentType.ReadOnly<VisualSortingTag>(),
                ComponentType.ReadOnly<SortingStaticTag>()
            );

            _sortingLayers = new List<SortingLayer>();
        }

        protected override void OnUpdate()
        {
            var sortingSpritesIsEmpty = _sortingSpritesQuery.IsEmpty;
            _sortingSpritesQuery.AddOrderVersionFilter();
            var sortingStaticSpritesIsEmpty = _sortingSpritesQuery.IsEmpty;
            _sortingSpritesQuery.ResetFilter();

            if (sortingSpritesIsEmpty && sortingStaticSpritesIsEmpty)
                return;

            _sortingLayers.Clear();
            EntityManager.GetAllUniqueSharedComponentsManaged(_sortingLayers);
            var bothModes = !sortingSpritesIsEmpty & !sortingSpritesIsEmpty;
            var handles = new NativeArray<JobHandle>(_sortingLayers.Count * (bothModes ? 2 : 1), Allocator.Temp);
            var staticHandlesOffset = bothModes ? _sortingLayers.Count : 0;

            if (!sortingSpritesIsEmpty)
            {
                for (int i = 0; i < _sortingLayers.Count; i++)
                {
                    var sortingLayer = _sortingLayers[i];
                    _sortingSpritesQuery.SetSharedComponentFilter(sortingLayer);
                    handles[i] = RegularSort(_sortingSpritesQuery, sortingLayer.index);
                }
            }

            if (!sortingStaticSpritesIsEmpty)
            {
                for (int i = 0; i < _sortingLayers.Count; i++)
                {
                    var sortingLayer = _sortingLayers[i];
                    _sortingStaticSpritesQuery.SetSharedComponentFilter(sortingLayer);
                    handles[staticHandlesOffset + i] = RegularSort(_sortingStaticSpritesQuery, sortingLayer.index);
                }
            }

            Dependency = JobHandle.CombineDependencies(handles);
        }
        private JobHandle RegularSort(in EntityQuery sortingQuery, int sortingLayer)
        {
            var spriteEntitiesCount = sortingQuery.CalculateEntityCount();

            if (spriteEntitiesCount == 0)
                return default;

            var sortingDataArray = new NativeArray<SortingData>(spriteEntitiesCount, Allocator.TempJob);
            // will use it to optimize sorting
            var dataPointers = new NativeArray<int>(spriteEntitiesCount, Allocator.TempJob);
            // will use it to write back result values
            var sortingValues = new NativeArray<SortingValue>(spriteEntitiesCount, Allocator.TempJob);

            var chunkBaseEntityIndeces = sortingQuery.CalculateBaseEntityIndexArrayAsync(Allocator.TempJob, default, out var calculateChunkBaseEntityIndeces);

            var gatherSortingDataJob = new GatherSortingDataJob
            {
                entityTypeHandle = GetEntityTypeHandle(),
                worldPosition2D_CTH = GetComponentTypeHandle<WorldPosition2D>(true),
                worldPosition2D_CDFE = GetComponentLookup<WorldPosition2D>(true),
                sortingIndex_CTH = GetComponentTypeHandle<SortingIndex>(true),
                pointers = dataPointers,
                sortingDataArray = sortingDataArray,
                chunkBasedEntityIndeces = chunkBaseEntityIndeces
            };
            var gatherSortingDataHandle = gatherSortingDataJob.ScheduleParallelByRef(sortingQuery, JobHandle.CombineDependencies(calculateChunkBaseEntityIndeces, Dependency));

            // after sorting dataPointers get sorted while sortingDataArray stay the same
            var sortHandle = new SortArrayJob<int, SortingDataComparer>
            {
                array = dataPointers,
                comparer = new()
                {
                    sourceData = sortingDataArray,
                    sourceDataComparer = new()
                }
            }.Schedule(gatherSortingDataHandle);

            _ = sortingDataArray.Dispose(sortHandle);

            var genSortingValuesJob = new GenerateSortingValuesJob
            {
                layerIndex = sortingLayer,
                sortingValues = sortingValues,
                pointers = dataPointers,
            }.ScheduleBatch(sortingValues.Length, 32, sortHandle);

            _ = dataPointers.Dispose(genSortingValuesJob);

            var writeBackChunkDataJob = new WriteSortingValuesToChunksJob
            {
                sortingValues = sortingValues,
                sortingValue_CTH_WO = GetComponentTypeHandle<SortingValue>(false),
                chunkBasedEntityIndeces = chunkBaseEntityIndeces
            };
            var writeBackChunkDataHandle = writeBackChunkDataJob.ScheduleParallelByRef(sortingQuery, genSortingValuesJob);

            _ = chunkBaseEntityIndeces.Dispose(writeBackChunkDataHandle);
            _ = sortingValues.Dispose(writeBackChunkDataHandle);

            return writeBackChunkDataHandle;
        }
    }
}