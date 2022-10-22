using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Profiling;

namespace NSprites
{
    public partial class RemoveOnKeySystem : SystemBase
    {
        protected override void OnUpdate()
        {
            if (!Input.GetKeyDown(KeyCode.R))
                return;

            Profiler.BeginSample("Remove On key");
            //var ecb = new EntityCommandBuffer(Allocator.TempJob);
            //var ecb_PW = ecb.AsParallelWriter();
            var removeList = new NativeList<Entity>(90000, Allocator.TempJob);
            var removeList_PW = removeList.AsParallelWriter();
            Entities.ForEach((Entity entity, int entityInQueryIndex, in RemoveOnKey removeOnKey) =>
            {
                //ecb_PW.RemoveComponent<RemoveOnKey>(entityInQueryIndex, entity);
                removeList_PW.AddNoResize(entity);
            }).ScheduleParallel();
            Dependency.Complete();
            EntityManager.RemoveComponent<RemoveOnKey>(removeList.AsArray());
            removeList.Dispose();
            //ecb.Playback(EntityManager);
            //ecb.Dispose();
            Profiler.EndSample();
        }
    }
}