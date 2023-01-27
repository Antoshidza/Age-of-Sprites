using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites
{
    public partial struct UpdateCullingDataSystem : ISystem
    {
        private class SystemData : IComponentData
        {
            private Camera _camera;

            public Camera Camera
            {
                get
                {
                    if(_camera == null)
                        _camera = Camera.main;
                    return _camera;
                }
            }
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
            var camera = state.EntityManager.GetComponentObject<SystemData>(state.SystemHandle).Camera;
            var leftBottomPoint = camera.ScreenToWorldPoint(new Vector3(0f, 0f, 0f));
            var rightUpPoint = camera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 0f));
            var cameraViewBounds = new float4(leftBottomPoint.x, rightUpPoint.x, leftBottomPoint.y, rightUpPoint.y);
            SystemAPI.SetSingleton(new SpriteFrustumCullingSystem.SystemData{ cullingBoudns = cameraViewBounds });
        }
    }
}