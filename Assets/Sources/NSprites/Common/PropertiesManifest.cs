using NSprites;

[assembly: InstancedPropertyComponent(typeof(MainTexST), "_mainTexSTBuffer", PropertyFormat.Float4)]
[assembly: InstancedPropertyComponent(typeof(WorldPosition2D), "_positionBuffer", PropertyFormat.Float2)]
[assembly: InstancedPropertyComponent(typeof(Scale2D), "_heightWidthBuffer", PropertyFormat.Float2)]
[assembly: InstancedPropertyComponent(typeof(Pivot), "_pivotBuffer", PropertyFormat.Float2)]
[assembly: InstancedPropertyComponent(typeof(SpriteSortingIndex), "_sortingIndexBuffer", PropertyFormat.Int)]

[assembly: DisableRenderingComponent(typeof(CullSpriteTag))]