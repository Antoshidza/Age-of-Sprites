using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    public partial class ReparentSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.Delete))
                return;

            Entities
                .ForEach((ref Parent2D parent, in ReparentTo reparentTo) =>
                {
                    parent.value = reparentTo.entity;
                }).ScheduleParallel();
        }
    }
}