using Unity.Entities;
using Unity.Mathematics;

namespace NSprites
{
    public partial class RandomColorSystem : SystemBase
    {
        private Random _random;
        private EntityQuery _randomColorQuery;

        protected override void OnCreate()
        {
            base.OnCreate();
            _random = new Random(1u);
            _randomColorQuery = GetEntityQuery
            (
                ComponentType.ReadOnly<RandomColor>(),
                ComponentType.ReadWrite<SpriteColor>()
            );
        }
        protected override void OnUpdate()
        {
            Entities
                .WithAll<RandomColor>()
                .ForEach((ref SpriteColor color) =>
                {
                    var randomVector = _random.NextFloat3();
                    color.color = new UnityEngine.Color(randomVector.x, randomVector.y, randomVector.z);
                })
                .WithoutBurst()
                .Run();

            EntityManager.RemoveComponent<RandomColor>(_randomColorQuery);
        }
    }
}
