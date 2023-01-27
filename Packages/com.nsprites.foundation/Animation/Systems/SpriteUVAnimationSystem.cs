using Unity.Mathematics;
using Unity.Entities;
using Unity.Burst;

#pragma warning disable CS0282 // I guess because of DOTS's codegen
// https://forum.unity.com/threads/compilation-of-issues-with-0-50.1253973/page-2#post-8512268

namespace NSprites
{
    /// Compare <see cref="AnimationTimer"/> with global time and switch <see cref="FrameIndex"/> when timer expired.
    /// Perform only not-culled entities. Restore <see cref="FrameIndex"/> and duration time for entities which be culled for some time.
    /// 
    /// Somehow calculations goes a bit wrong and unculled entities gets synchronyzed, don't know how to fix
    [BurstCompile]
    public partial struct SpriteUVAnimationSystem : ISystem
    {
        [BurstCompile]
        [WithNone(typeof(CullSpriteTag))]
        private partial struct AnimationJob : IJobEntity
        {
            public double time;

            private void Execute(ref AnimationTimer animationTimer,
                                    ref FrameIndex frameIndex,
                                    ref MainTexST mainTexST,
                                    in AnimationSetLink animationSet,
                                    in AnimationIndex animationIndex)
            {
                var timerDelta = time - animationTimer.value;

                if (timerDelta >= 0f)
                {
                    ref var animData = ref animationSet.value.Value[animationIndex.value];
                    var frameCount = animData.GridSize.x * animData.GridSize.y;
                    frameIndex.value = (frameIndex.value + 1) % frameCount;
                    var nextFrameDuration = animData.FrameDurations[frameIndex.value];

                    if (timerDelta >= animData.AnimationDuration)
                    {
                        var extraTime = (float)(timerDelta % animData.AnimationDuration);
                        while (extraTime > nextFrameDuration)
                        {
                            extraTime -= nextFrameDuration;
                            frameIndex.value = (frameIndex.value + 1) % frameCount;
                            nextFrameDuration = animData.FrameDurations[frameIndex.value];
                        }
                        nextFrameDuration -= extraTime;
                    }

                    animationTimer.value = time + nextFrameDuration;

                    var frameSize = new float2(animData.MainTexSTOnAtlas.xy / animData.GridSize);
                    var framePosition = new int2(frameIndex.value % animData.GridSize.x, frameIndex.value / animData.GridSize.x);
                    mainTexST = new MainTexST { value = new float4(frameSize, animData.MainTexSTOnAtlas.zw + frameSize * framePosition) };
                }
            }
        }

        public void OnCreate(ref SystemState state)
        {
        }

        public void OnDestroy(ref SystemState state)
        {
        }
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var animationJob = new AnimationJob { time = SystemAPI.Time.ElapsedTime };
            state.Dependency = animationJob.ScheduleParallelByRef(state.Dependency);
        }
    }
}