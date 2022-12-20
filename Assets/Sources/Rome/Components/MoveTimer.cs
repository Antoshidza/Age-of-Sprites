using Unity.Entities;

public struct MoveTimer : IComponentData, IEnableableComponent
{
    public float remainingTime;
}