using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;

namespace NSprites
{
    [BurstCompile]
    public partial struct SpriteFrustumCullingSystem : ISystem
    {
        [BurstCompile]
        [WithAll(typeof(SpriteRenderID))]
        [WithNone(typeof(CullSpriteTag))]
        private partial struct DisableCulledJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            public float4 cameraViewBounds;

            private void Execute(Entity entity, [ChunkIndexInQuery]int chunkIndex, in WorldPosition2D worldPosition, in Scale2D size, in Pivot pivot)
            {
                var viewPosition = worldPosition.value - size.value * pivot.value;
                if (!IsInsideCameraBounds(GetRect(viewPosition, size.value), cameraViewBounds))
                    ecb.AddComponent<CullSpriteTag>(chunkIndex, entity);
            }
        }
        [BurstCompile]
        [WithAll(typeof(SpriteRenderID))]
        [WithAll(typeof(CullSpriteTag))]
        private partial struct EnableUnculledJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter ecb;
            public float4 cameraViewBounds;

            private void Execute(Entity entity, [ChunkIndexInQuery] int chunkIndex, in WorldPosition2D worldPosition, in Scale2D size, in Pivot pivot)
            {
                var viewPosition = worldPosition.value - size.value * pivot.value;
                if (IsInsideCameraBounds(GetRect(viewPosition, size.value), cameraViewBounds))
                    ecb.RemoveComponent<CullSpriteTag>(chunkIndex, entity);
            }
        }
        public struct SystemData : IComponentData
        {
            public float4 cullingBoudns;
        }

#if UNITY_EDITOR
        [MenuItem("NSprites/Toggle frustum culling system")]
        public static void ToggleFrustumCullingSystem()
        {
            var systemHandle = World.DefaultGameObjectInjectionWorld.GetExistingSystem<SpriteFrustumCullingSystem>();

            if (systemHandle == null)
                return;

            var systemState =  World.DefaultGameObjectInjectionWorld.Unmanaged.ResolveSystemStateRef(systemHandle);

            systemState.Enabled = !systemState.Enabled;

            if (!systemState.Enabled)
                systemState.EntityManager.RemoveComponent(systemState.GetEntityQuery(typeof(CullSpriteTag)), ComponentType.ReadOnly<CullSpriteTag>());
        }
#endif
        private static float4 GetRect(in float2 position, in float2 size)
        {
            var leftBottomPoint = position;
            var rightUpPoint = position + size;
            return new float4(leftBottomPoint.x, rightUpPoint.x, leftBottomPoint.y, rightUpPoint.y);
        }
        private static bool IsInsideCameraBounds(in float2 position, in float4 cameraViewBounds)
        {
            return position.x > cameraViewBounds.x &&
                position.x < cameraViewBounds.y &&
                position.y > cameraViewBounds.z &&
                position.y < cameraViewBounds.w;
        }
        private static bool IsInsideCameraBounds(in float4 rect, in float4 cameraViewBounds)
        {
            return IsInsideCameraBounds(new float2(rect.x, rect.z), cameraViewBounds) ||
                IsInsideCameraBounds(new float2(rect.x, rect.w), cameraViewBounds) ||
                IsInsideCameraBounds(new float2(rect.y, rect.z), cameraViewBounds) ||
                IsInsideCameraBounds(new float2(rect.y, rect.w), cameraViewBounds);
        }
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _ = state.EntityManager.AddComponentData(state.SystemHandle, new SystemData());
        }

        public void OnDestroy(ref SystemState state)
        {
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();
            var cullingBounds = SystemAPI.GetComponent<SystemData>(state.SystemHandle).cullingBoudns;

            var disableCulledJob = new DisableCulledJob
            {
                cameraViewBounds = cullingBounds,
                ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency =  disableCulledJob.ScheduleParallelByRef(state.Dependency);

            var enableUnculledJob = new EnableUnculledJob
            {
                cameraViewBounds = cullingBounds,
                ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency = enableUnculledJob.ScheduleParallelByRef(state.Dependency);
        }
    }
}
