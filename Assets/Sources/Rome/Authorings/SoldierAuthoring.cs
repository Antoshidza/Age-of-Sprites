using Unity.Entities;
using UnityEngine;

public class SoldierAuthoring : MonoBehaviour
{
    private class SoldierBaker : Baker<SoldierAuthoring>
    {
        public override void Bake(SoldierAuthoring authoring)
        {
            AddComponent<SoldierTag>();
            AddComponent<Destination>();
            AddComponent<MoveTimer>();
            AddComponent(new MoveSpeed { value = authoring.MoveSpeed });
        }
    }

    public float MoveSpeed;
}