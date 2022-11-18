using Unity.Entities;
using UnityEngine;

public struct SpawnSoliderSystem : ISystem
{
    private EntityCommandBufferSystem _ecbSystem;

    public void OnCreate(ref SystemState state)
    {
        _ecbSystem = state.World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        if (!Input.GetKey(KeyCode.A))
            return;

        if (!state.TryGetSingleton<SquadDefaultSettings>(out var squadSettings))
            return;

        var ecb = _ecbSystem.CreateCommandBuffer();

        ecb.Instantiate(squadSettings.soldierPrefab);
    }
}