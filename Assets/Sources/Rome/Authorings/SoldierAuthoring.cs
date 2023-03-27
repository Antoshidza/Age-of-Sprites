using Unity.Entities;
using UnityEngine;

public class SoldierAuthoring : MonoBehaviour
{
    private class SoldierBaker : Baker<SoldierAuthoring>
    {
        public override void Bake(SoldierAuthoring authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<SoldierTag>(entity);
            AddComponent<Destination>(entity);
            AddComponent<MoveTimer>(entity);
            AddComponent(entity, new MoveSpeed { value = authoring.MoveSpeed });
        }
    }

    public float MoveSpeed;
}