using NSprites;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

public partial struct RandomColorSystem : ISystem
{
    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var deltaTime = state.Time.DeltaTime;

        state.Dependency = state.Entities
            //.WithNone<CullSpriteTag>()
            .ForEach((ref SpriteColor color, ref RandomColor random) =>
            {
                Color.RGBToHSV(color.color, out var hue, out _, out _);
                var nextHue = math.lerp(hue, random.randHue, random.timer == 0f ? 1f : deltaTime / random.timer);
                color.color = Color.HSVToRGB(nextHue, .8f, .9f);
                random.timer -= deltaTime;

                if (random.timer > 0f)
                    return;

                random.randHue = random.rand.NextFloat(0f, 1f);
                random.timer = math.length(hue - random.randHue);
            }).ScheduleParallel(state.Dependency);
    }
}