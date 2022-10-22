using System.Collections;
using Unity.Entities;
using UnityEngine;

namespace NSprites
{
    public partial class DestroyOnKeySystem : SystemBase
    {
        private EntityCommandBufferSystem _ecbSystem;

        protected override void OnCreate()
        {
            base.OnCreate();
            _ecbSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
        }
        protected override void OnUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.Delete))
                return;
            var ecb = _ecbSystem.CreateCommandBuffer().AsParallelWriter();

            Entities
                .ForEach((Entity entity, int entityInQueryIndex, in DestroyOnKey destroyOnKey) =>
                {
                    if (destroyOnKey.value == KeyCode.Delete)
                        ecb.DestroyEntity(entityInQueryIndex, entity);
                }).ScheduleParallel();

            _ecbSystem.AddJobHandleForProducer(Dependency);
        }
    }
}