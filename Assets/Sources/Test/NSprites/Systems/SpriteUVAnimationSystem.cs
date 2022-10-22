using NSprites;
using Unity.Mathematics;
using Unity.Entities;

public partial class SpriteUVAnimationSystem : SystemBase
{
    private const float frameDuration = 0.25f;
    protected override void OnUpdate()
    {
        var deltaTime = Time.DeltaTime;
        Entities
            .ForEach((ref AnimationTimer animationTimer,
                                ref FrameIndex frameIndex,
                                ref MainTexST mainTexST,
                                in FrameGrid frameGrid,
                                in MainTexSTInitial mainTexSTInitial) =>
            {
                animationTimer.value -= deltaTime;
                if(animationTimer.value <= 0f)
                {
                    animationTimer.value += frameDuration;
                    frameIndex.value = (frameIndex.value + 1) % (frameGrid.size.x * frameGrid.size.y);
                    var frameSize = new float2(mainTexSTInitial.value.xy / frameGrid.size);
                    var framePosition = new int2(frameIndex.value % frameGrid.size.x, frameIndex.value / frameGrid.size.x);
                    mainTexST = new MainTexST { value = new float4(frameSize, mainTexSTInitial.value.zw + frameSize * framePosition) };
                }
            })
            .ScheduleParallel();
    }
}
