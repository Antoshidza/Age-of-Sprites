using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Serialization;

public class FactoryAuthoring : MonoBehaviour
{
    private class FactoryBaker : Baker<FactoryAuthoring>
    {
        public override void Bake(FactoryAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent
            (
                entity,
                new FactoryData
                {
                    prefab = GetEntity(authoring.Prefab, TransformUsageFlags.None),
                    instantiatePos = new float2(authoring.transform.position.x, authoring.transform.position.y) + authoring.SpawnOffset,
                    count = authoring.SpawnCount,
                    duration = authoring.Duration
                }
            );
            AddComponent(entity, new FactoryTimer { value = authoring.RandomInitialDuration ? UnityEngine.Random.Range(0f, authoring.Duration) : authoring.Duration });
        }
    }

    [FormerlySerializedAs("_prefab")] public GameObject Prefab;
    [FormerlySerializedAs("_spawnOffset")] public float2 SpawnOffset;
    [FormerlySerializedAs("_duration ")] public float Duration = 1f;
    [FormerlySerializedAs("_spawnCount ")] public int SpawnCount = 1;
    [FormerlySerializedAs("_randomInitialDuration")] public bool RandomInitialDuration;
}