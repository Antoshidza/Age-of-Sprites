using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using static NSprites.SpriteSortingSystem;

[assembly: RegisterGenericJobType(typeof(SortArrayJob<int, SortingDataComparer>))]

namespace NSprites
{
    [BurstCompile]
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.Editor)]
    public partial struct SpriteSortingSystem : ISystem
    {
        #region data
        private struct SystemData : IComponentData
        {
            public EntityQuery sortingSpritesQuery;
            public EntityQuery sortingStaticSpritesQuery;
        }
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

        private JobHandle RegularSort(in EntityQuery sortingQuery, int sortingLayer, ref SystemState state)
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
                entityTypeHandle = SystemAPI.GetEntityTypeHandle(),
                worldPosition2D_CTH = SystemAPI.GetComponentTypeHandle<WorldPosition2D>(true),
                worldPosition2D_CDFE = SystemAPI.GetComponentLookup<WorldPosition2D>(true),
                sortingIndex_CTH = SystemAPI.GetComponentTypeHandle<SortingIndex>(true),
                pointers = dataPointers,
                sortingDataArray = sortingDataArray,
                chunkBasedEntityIndeces = chunkBaseEntityIndeces
            };
            var gatherSortingDataHandle = gatherSortingDataJob.ScheduleParallelByRef(sortingQuery, JobHandle.CombineDependencies(calculateChunkBaseEntityIndeces, state.Dependency));

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
                sortingValue_CTH_WO = SystemAPI.GetComponentTypeHandle<SortingValue>(false),
                chunkBasedEntityIndeces = chunkBaseEntityIndeces
            };
            var writeBackChunkDataHandle = writeBackChunkDataJob.ScheduleParallelByRef(sortingQuery, genSortingValuesJob);

            _ = chunkBaseEntityIndeces.Dispose(writeBackChunkDataHandle);
            _ = sortingValues.Dispose(writeBackChunkDataHandle);

            return writeBackChunkDataHandle;
        }

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            var systemData = new SystemData();
            var queryBuilder = new EntityQueryBuilder(Allocator.Temp)
                .WithNone<CullSpriteTag>()
                .WithAll<WorldPosition2D>()
                .WithAll<SortingValue>()
                .WithAll<SortingIndex>()
                .WithAll<SortingLayer>()
                .WithAll<VisualSortingTag>()
                .WithNone<SortingStaticTag>();
            systemData.sortingSpritesQuery = state.GetEntityQuery(queryBuilder);

            queryBuilder.Reset();
            _ = queryBuilder.WithNone<CullSpriteTag>()
                .WithAll<WorldPosition2D>()
                .WithAll<SortingValue>()
                .WithAll<SortingIndex>()
                .WithAll<SortingLayer>()
                .WithAll<VisualSortingTag>()
                .WithAll<SortingStaticTag>();
            systemData.sortingStaticSpritesQuery = state.GetEntityQuery(queryBuilder);

            _ = state.EntityManager.AddComponentData(state.SystemHandle, systemData);

            queryBuilder.Dispose();
        }

        public void OnDestroy(ref SystemState state)
        {
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {

            var systemData = SystemAPI.GetComponent<SystemData>(state.SystemHandle);
            var sortingSpritesIsEmpty = systemData.sortingSpritesQuery.IsEmpty;
            systemData.sortingSpritesQuery.AddOrderVersionFilter();
            var sortingStaticSpritesIsEmpty = systemData.sortingSpritesQuery.IsEmpty;
            systemData.sortingSpritesQuery.ResetFilter();

            if (sortingSpritesIsEmpty && sortingStaticSpritesIsEmpty)
                return;

            state.EntityManager.GetAllUniqueSharedComponents<SortingLayer>(out var sortingLayers, Allocator.Temp);
            var bothModes = !sortingSpritesIsEmpty & !sortingSpritesIsEmpty;
            var handles = new NativeArray<JobHandle>(sortingLayers.Length * (bothModes ? 2 : 1), Allocator.Temp);
            var staticHandlesOffset = bothModes ? sortingLayers.Length : 0;

            if (!sortingSpritesIsEmpty)
            {
                for (int i = 0; i < sortingLayers.Length; i++)
                {
                    var sortingLayer = sortingLayers[i];
                    systemData.sortingSpritesQuery.SetSharedComponentFilter(sortingLayer);
                    handles[i] = RegularSort(systemData.sortingSpritesQuery, sortingLayer.index, ref state);
                }
            }

            if (!sortingStaticSpritesIsEmpty)
            {
                for (int i = 0; i < sortingLayers.Length; i++)
                {
                    var sortingLayer = sortingLayers[i];
                    systemData.sortingStaticSpritesQuery.SetSharedComponentFilter(sortingLayer);
                    handles[staticHandlesOffset + i] = RegularSort(systemData.sortingStaticSpritesQuery, sortingLayer.index, ref state);
                }
            }

            state.Dependency = JobHandle.CombineDependencies(handles);
        }
    }
}