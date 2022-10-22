using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace NSprites
{
    public partial class MoveWithCursor : SystemBase
    {
        protected override void OnUpdate()
        {
            var mousePos = ((float3)Camera.main.ScreenToWorldPoint(Input.mousePosition)).xy;

            Entities
                .WithAll<MoveWithCursorTag>()
                .ForEach((ref WorldPosition2D worldPosition) =>
                {
                    worldPosition.value = mousePos;
                }).ScheduleParallel();
        }
    }
}
