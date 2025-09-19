# Minecraft Shader Suite

This folder collects maintainable shaders that cover the visual needs of the voxel world. Each shader is authored in built-in Unity syntax so it can be edited without Shader Graph and kept under version control easily.

## Shader Overview

- `Minecraft/VoxelBlockStandard` — Opaque block material that samples a texture atlas and supports lightweight ambient occlusion tinting. Works with GPU instancing and per-face UV offsets.
- `Minecraft/WaterSurface` — Transparent water surface with depth-based colouring, scrolling normal maps, and simple wave distortion. Blend mode is additive-alpha for quick setup.
- `Minecraft/FoliageWind` — Cutout shader for leaves and tall grass. Applies vertex wind animation and tint variation for per-instance variety.
- `Minecraft/SkyGradient` — Procedural sky dome using a vertical gradient and sun direction tint. Designed for lightweight day-night transitions.

## Usage Notes

1. Place the shaders under a `Resources` or `Addressables` path if runtime loading is required.
2. Create materials from each shader and assign them to the relevant renderers (chunk meshes, water planes, foliage billboards, sky dome mesh).
3. For `VoxelBlockStandard`, use `MaterialPropertyBlock` to supply the `_UVData` vector (`xy` offset, `zw` scale) for each submesh/face.
4. For water and foliage, the wind parameters can be driven by a shared global script through `Shader.SetGlobalVector`.

Each shader contains extensive inline comments and `#pragma multi_compile_instancing` so that expandability is straightforward.
