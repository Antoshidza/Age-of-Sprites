# Age of Sprites - Sample project for [NSprites](https://github.com/Antoshidza/NSprites) package

![RomeGIf](https://user-images.githubusercontent.com/19982288/204523105-7cabb122-954c-4fb0-97bc-becb27d2d2b9.gif)

## What this project solves
* Registering render for each sprite
* Registering properties-components for NSprites rendering
* Rendering of all entities-sprites within 1 drawcall using texture atlas and passing tiling-and-offset values for each entity
* Culling sprites outside of camera bounds
* Screen sorting sprites with layers and dynamic / static sorting
* Animating sprites using tiling-and-offset values change
* Implementing 2D transforms to avoid unecessary data
* +Poor example of producing units and building squads for strategy games

## Requirements
* Unity 2022.2.3+
* Entities v1.0.0-pre.65
* [NSprites v3.1.0+](https://github.com/Antoshidza/NSprites/releases/tag/v3.1.0)

## Using [NSprites-Foundation](https://github.com/Antoshidza/NSprites-Foundation)
Common solutions was step by step moved from here to separate [NSprites-Foundation](https://github.com/Antoshidza/NSprites-Foundation/blob/main/Base/PropertiesManifest.cs) repo to be reused as a package.
So solutions described below often references stuff which was initialy created inside this repo, though all links was edited to lead to foundation repo.

## How register render happens
For this project I've used adding managed component to sprite entity to be able to register unique renders at runtime. Such solution doesn't scale well with growing count of existing sprites in scene. Though there are few possibilities to solve problem of managed components such as using components with fixed string GUID and then load managed data with AssetDatabase.

## How register properties-components happens
There is nothing to say more then in [docs](https://github.com/Antoshidza/NSprites/wiki/Register-components-as-properties). All registering comes with happens in [PropertiesManifest.cs](https://github.com/Antoshidza/NSprites-Foundation/blob/main/Base/PropertiesManifest.cs).

## How rendering goes within 1 drawcall for different sprites
First thing is to use sprite atlasing. Then we come up with question how to render instances with same texture. Answer is simple - we just need pas texture's Tiling&Offset as property. I've used [Reactive](https://github.com/Antoshidza/NSprites/wiki/Property-update-modes) property type, because animation system writes to this component, so we need to sync updated data, even if some sprites never change theirs Tiling&Offset property. Then in [shader](https://github.com/Antoshidza/NSprites-Foundation/blob/main/Graphics/Regular%20Render/RegularNSprites_Shader.shader) we use simple function to locate our sprite on atlas
```hlsl
float2 TilingAndOffset(float2 UV, float2 Tiling, float2 Offset)
{
    return UV * Tiling + Offset;
}
// ...
varyings.uv = TilingAndOffset(attributes.uv, mainTexST.xy, mainTexST.zw);
```
You can easily obtain Tiling&Offset values using `NSpritesUtils.GetTextureST(Sprite)`.

## How screen sorting happens
[Shader](https://github.com/Antoshidza/NSprites-Foundation/blob/main/Graphics/Regular%20Render/RegularNSprites_Shader.shader) makes a little trick described in this [thread](https://forum.unity.com/threads/how-to-sort-2d-objects-with-same-z-position-in-shader.1347008/#post-8506538). It recieves some **sorting data** which is a `int2` with layer and sorting indexes, and writes to `SV_POSITION.z` with calculated for each instances sorting value.

## How animation happens
Sprite per-frame animation is just changing sprite we render, so [animation system](https://github.com/Antoshidza/NSprites-Foundation/blob/main/About/Animation.md) just changes `UVAtlas` values over time. There is banch of `ScriptableObject`s and authorings to prepare animation data. It just contains frame count / distribution / duration / etc. At the end all data goes to blob assets.
The best solution would be use `HashMap` in blob, but there is no built-in solution and community solutions unsupported, so every time I need switch animation I perform searching in blob array, though it can be improved.

## What happens in this example
There is few tents which are factories for units. They produce units over time. Free units search for squad which need units and then recieves it's position in squad. After all squads was filled with units new squad spawns. That is all. 
>Tip: you can navigate to NSprites -> **Toggle draw squads for View window** to see where squads exists, but it is expansive part.
