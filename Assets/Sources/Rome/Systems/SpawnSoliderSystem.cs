using Unity.Burst;
using Unity.Entities;
using UnityEngine;

[BurstCompile]
public partial struct SpawnSoliderSystem : ISystem
{
    [BurstCompile]
    public void OnUpdate(ref SystemState state)
    {
        if (!Input.GetKey(KeyCode.A))
            return;

        if (!SystemAPI.TryGetSingleton<SquadDefaultSettings>(out var squadSettings))
            return;

        _ = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).Instantiate(squadSettings.soldierPrefab);
    }
}