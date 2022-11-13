using NSprites;
using Unity.Entities;
using Unity.Mathematics;

public partial struct SquadMoveSystem : ISystem
{
    private SquadSettings _prevSquadSettings;

    public void OnCreate(ref SystemState state)
    {
    }

    public void OnDestroy(ref SystemState state)
    {
    }

    public void OnUpdate(ref SystemState state)
    {
        var squadSettings = state.GetSingleton<SquadSettings>();

        var destination_CDFE = state.GetComponentDataFromEntity<Destination>();

        if (_prevSquadSettings != squadSettings)
        {
            _prevSquadSettings = squadSettings;

            state.Dependency = state.Entities
            .WithNativeDisableParallelForRestriction(destination_CDFE)
            .ForEach((in WorldPosition2D pos, in DynamicBuffer<SoldierLink> soldiersBuffer) =>
            {
                var perSoldierOffset = (2 * squadSettings.soldierMargin + 1f) * squadSettings.soldierSize;

                for (int soldierIndex = 0; soldierIndex < soldiersBuffer.Length; soldierIndex++)
                    destination_CDFE[soldiersBuffer[soldierIndex].entity] = new Destination { value = pos.value + (perSoldierOffset * new int2(soldierIndex % squadSettings.squadResolution.x, soldierIndex / squadSettings.squadResolution.x)) };
            }).ScheduleParallel(state.Dependency);
        }
        else
            state.Dependency = state.Entities
                .WithNativeDisableParallelForRestriction(destination_CDFE)
                .WithChangeFilter<WorldPosition2D>()
                .ForEach((ref PrevWorldPosition2D prevPos, in WorldPosition2D pos, in DynamicBuffer<SoldierLink> soldiersBuffer) =>
                {
                    if (math.any(prevPos.value != pos.value))
                    {
                        prevPos.value = pos.value;

                        var perSoldierOffset = (2 * squadSettings.soldierMargin + 1f) * squadSettings.soldierSize;

                        for (int soldierIndex = 0; soldierIndex < soldiersBuffer.Length; soldierIndex++)
                            destination_CDFE[soldiersBuffer[soldierIndex].entity] = new Destination { value = pos.value + (perSoldierOffset * new int2(soldierIndex % squadSettings.squadResolution.x, soldierIndex / squadSettings.squadResolution.x)) };
                    }
                }).ScheduleParallel(state.Dependency);
    }
}