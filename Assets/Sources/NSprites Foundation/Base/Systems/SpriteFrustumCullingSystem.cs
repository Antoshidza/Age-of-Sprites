using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;

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
        private class SystemData : IComponentData
        {
            private Camera _camera;

            public Camera Camera => _camera == null ? Camera.main : _camera;
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

        public void OnCreate(ref SystemState state)
        {
            state.EntityManager.AddComponentObject(state.SystemHandle, new SystemData());
        }

        public void OnDestroy(ref SystemState state)
        {
        }

        public void OnUpdate(ref SystemState state)
        {
            var systemData = state.EntityManager.GetComponentObject<SystemData>(state.SystemHandle);
            var ecbSingleton = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>();

            var leftBottomPoint = systemData.Camera.ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
            var rightUpPoint = systemData.Camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0f));
            var cameraViewBounds = new float4(leftBottomPoint.x, rightUpPoint.x, leftBottomPoint.y, rightUpPoint.y);

            var disableCulledJob = new DisableCulledJob
            {
                cameraViewBounds = cameraViewBounds,
                ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency =  disableCulledJob.ScheduleParallelByRef(state.Dependency);

            var enableUnculledJob = new EnableUnculledJob
            {
                cameraViewBounds = cameraViewBounds,
                ecb = ecbSingleton.CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency = enableUnculledJob.ScheduleParallelByRef(state.Dependency);
        }
    }
}
