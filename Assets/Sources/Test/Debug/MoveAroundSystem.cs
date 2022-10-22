using NSprites;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial class MoveAroundSystem : SystemBase
{
    private const float DISTANCE_THRESHOLD = 0.1f;

    private Camera _mainCamera;

    protected override void OnUpdate()
    {
        if(_mainCamera == null)
            _mainCamera = Camera.main;

        var time = Time.ElapsedTime;
        Entities
            .ForEach((ref WorldPosition2D worldPosition, in MoveAround moveAround) =>
            {
                worldPosition.value = moveAround.startPosition + moveAround.area * (float)math.sin(time + moveAround.timeOffset);
            }).ScheduleParallel();

        var deltaTime = Time.DeltaTime;
        var leftBottomScreenPosition = ((float3)_mainCamera.ScreenToWorldPoint(new Vector3(0f, 0f, 1f))).xy;
        var upRightScreenPosition = ((float3)_mainCamera.ScreenToWorldPoint(new Vector3(Screen.width, Screen.height, 1f))).xy;
        Entities
            .ForEach((ref WorldPosition2D worldPosition, ref MoveRandom moveRandom, ref MoveAroundScreen moveAround) =>
            {
                worldPosition.value = math.lerp(worldPosition.value, moveAround.destination, deltaTime);

                if(math.distance(worldPosition.value, moveAround.destination) < DISTANCE_THRESHOLD)
                    moveAround.destination = moveRandom.random.NextFloat2(leftBottomScreenPosition, upRightScreenPosition);
            }).ScheduleParallel();

        var ecb = new EntityCommandBuffer(Unity.Collections.Allocator.Temp);
        Entities
            .WithAll<MoveAroundScreen>()
            .WithNone<MoveRandom>()
            .ForEach((Entity entity) =>
            {
                ecb.AddComponent(entity, new MoveRandom { random = new Unity.Mathematics.Random((uint)UnityEngine.Random.Range(1, int.MaxValue)) });
            })
            .WithoutBurst()
            .Run();
        ecb.Playback(EntityManager);
    }
}
