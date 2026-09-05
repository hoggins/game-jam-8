# Environment visibility culling

`MapEnvironmentSpawner` uses one Unity `CullingGroup` entry per logical spawned object. Each entry
has an aggregated `BoundingSphere` and a `Renderer[]` containing the object's renderers and child
renderers. Unity checks the spheres for camera visibility and distance; a callback enables or
disables the renderer group. There is no per-frame scan of the spawned-object collection.

## Registering another object type

For a new static object type (for example, trees or rocks):

1. Spawn it through `MapEnvironmentSpawner` or its shared spawn path.
2. Give it one logical root `GameObject` and put all visual child renderers below that root.
3. Add it to the placement loop and call `RegisterRenderers(index, instance)` once after
   instantiation. Keep `index` aligned with the `BoundingSphere[]` and renderer arrays.
4. Calculate the sphere from all child renderers, as the existing registration does. Keep the
   object's colliders and gameplay components enabled; visibility culling only affects renderers.

The current helper is private because the existing map spawner owns the registration arrays. If a
separate spawner must register objects, extract the same arrays and callback logic into a shared
registry rather than adding another per-frame visibility service.

## Settings and special cases

Tune `Assets/Resources/EnvironmentVisibilitySettings.asset`:

- `visibleRadius`: objects inside this radius can be rendered.
- `hiddenMargin`: extra distance before an object is hidden again, preventing boundary flicker.

Moving objects need their bounding sphere updated when they move, or should use a separate dynamic
registry. Objects without renderers are not affected. Objects that need to remain visible to a
secondary camera or minimap should not be registered without adapting the visibility policy.
