using NSprites;
using Unity.Mathematics;
using Unity.Entities;

public partial struct SpriteUVAnimationSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var time = state.Time.ElapsedTime;

        state.Dependency = state.Entities
            //.WithNone<CullSpriteTag>()
            .ForEach((ref AnimationTimer animationTimer,
                                ref FrameIndex frameIndex,
                                ref MainTexST mainTexST,
                                in AnimationDataLink animation) =>
            {
                var timerDelta = time - animationTimer.value;

                if (timerDelta >= 0f)
                {
                    ref var animData = ref animation.value.Value;
                    var frameCount = animData.GridSize.x * animData.GridSize.y;
                    frameIndex.value = (frameIndex.value + 1) % frameCount;
                    var nextFrameDuration = animData.FrameDurations[frameIndex.value];

                    if (timerDelta >= animData.AnimationDuration)
                    {
                        var prevIndex = frameIndex.value;
                        var prevFrameDuration = animData.FrameDurations[frameIndex.value];

                        var extraTime = (float)(timerDelta % animData.AnimationDuration);
                        while (extraTime > nextFrameDuration)
                        {
                            extraTime -= nextFrameDuration;
                            frameIndex.value = (frameIndex.value + 1) % frameCount;
                            nextFrameDuration = animData.FrameDurations[frameIndex.value];
                        }
                        nextFrameDuration -= extraTime;

                        //Debug.Log($"{timerDelta} > {animData.AnimationDuration}. extraTime: {(float)(timerDelta % animData.AnimationDuration)}. index: {prevIndex} -> {frameIndex.value}, fd: {prevFrameDuration} -> {nextFrameDuration}");
                    }

                    animationTimer.value = time + nextFrameDuration;

                    var frameSize = new float2(animData.MainTexSTOnAtlas.xy / animData.GridSize);
                    var framePosition = new int2(frameIndex.value % animData.GridSize.x, frameIndex.value / animData.GridSize.x);
                    mainTexST = new MainTexST { value = new float4(frameSize, animData.MainTexSTOnAtlas.zw + frameSize * framePosition) };
                }
            })
            .ScheduleParallel(state.Dependency);
    }
}
