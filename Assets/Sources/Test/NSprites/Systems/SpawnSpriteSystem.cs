using Unity.Entities;
using NSprites;
using UnityEngine;
using Unity.Collections;

public partial class SpawnSpriteSystem : SystemBase
{
    private EntityQuery _spritePrefabQuery;
    private Unity.Mathematics.Random _rand;

    protected override void OnCreate()
    {
        base.OnCreate();
        _spritePrefabQuery = GetEntityQuery
        (
            ComponentType.ReadOnly<Prefab>(),
            ComponentType.ReadOnly<SpriteRendererTag>(),
            ComponentType.Exclude<Parent2D>()
        );
        _rand = new Unity.Mathematics.Random(1u);
    }
    protected override void OnUpdate()
    {
        if(Input.GetKey(KeyCode.Z))
        {
            var prefabs = _spritePrefabQuery.ToEntityArray(Allocator.Temp);
            void Spawn(in int amount)
            {
                for(int i = 0; i <= amount; i++)
                    _ = EntityManager.Instantiate(prefabs[_rand.NextInt(0, prefabs.Length)]);
            }
            Spawn(100);
        }
    }
}