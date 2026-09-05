# Scene HUD objects

A scene HUD object is a real, destructible object standing in the world that is mirrored onto the
battle HUD. There is no separate HUD state model: a camera parented to the object renders it into a
`RenderTexture`, and a `RawImage` on the HUD draws that texture. Smashing the object in the world is
immediately visible on the HUD, because the HUD *is* the object.

Two are implemented today: `SceneHudElementId.BattleTimer` (the battle clock) and
`SceneHudElementId.Arrow` (the compass pointing at the clock — see [The compass arrow](#the-compass-arrow)).
Design intent for the rest — HP, Ammo, Upgrade — is in [../Design/HudObjects.md](../Design/HudObjects.md).

## Types

| File | Role |
| --- | --- |
| [Assets/Scripts/SceneHud/SceneHudElementId.cs](../Assets/Scripts/SceneHud/SceneHudElementId.cs) | The enum that pairs a world object with its HUD widget. Neither side references the other. |
| [Assets/Scripts/SceneHud/SceneHudElement.cs](../Assets/Scripts/SceneHud/SceneHudElement.cs) | World side. Sits on the camera, owns the `RenderTexture`, registers it. |
| [Assets/Scripts/SceneHud/SceneHudElementView.cs](../Assets/Scripts/SceneHud/SceneHudElementView.cs) | HUD side. `RequireComponent(RawImage)`; shows the texture, hides itself when there is none. |
| [Assets/Scripts/SceneHud/SceneHudService.cs](../Assets/Scripts/SceneHud/SceneHudService.cs) | `SceneHudElementId -> RenderTexture` registry plus a `Changed` event. |

`SceneHudService` is a plain singleton registered in
[AppScope.Configure](../Assets/Scripts/App/AppScope.cs). The HUD widget lives on
`Assets/Resources/Prefabs/UI/Battle/BattleHud.prefab`.

## How it reaches the HUD

1. `SceneHudElement.OnEnable` allocates a `RenderTexture` (`ARGB32`, 16-bit depth) sized by its
   `_resolution` field, assigns it to `Camera.targetTexture`, and calls
   `SceneHudService.Register(_id, texture)`.
2. `Register` stores the texture under the id and raises `Changed`.
3. `SceneHudElementView.OnEnable` subscribes to `Changed` and seeds itself with
   `SceneHudService.Get(_id)`. `Apply` assigns `RawImage.texture` and sets `RawImage.enabled` to
   whether a texture exists — so a destroyed or not-yet-spawned object leaves no stale frame.
4. `SceneHudElement.OnDisable` calls `Unregister(_id, _texture)`, then releases and destroys the
   texture.

`Register` overwrites by id, and `Unregister` only clears the entry when the texture passed in is
still the current one. That combination is what makes replacement safe: when an object respawns, the
new element registers before the old one tears down, and the old one's unregister is ignored instead
of blanking the HUD. Preserve that ordering — destroy or disable the outgoing object *after* the
replacement exists, not before.

## Adding a new scene HUD object

### 1. Declare the id

Add a value to `SceneHudElementId`.

### 2. Add the HUD widget

On `BattleHud.prefab`, add a `RawImage` with a `SceneHudElementView` and set its `_id`. Nothing else
is needed; it finds its texture by id.

### 3. Build the world prefab's camera

Add a child GameObject with `Camera` + `UniversalAdditionalCameraData` + `SceneHudElement`. Copy the
settings from `BattleTimerPrefabBuilder.BuildHudCamera`:

- `orthographic = true`, `orthographicSize` = half the framed depth.
- Positioned above the object, `localRotation = Euler(90, 0, 0)` — looking straight down, up vector
  along +Z so the object reads the same way it does under the top-down battle camera.
- `clearFlags = SolidColor` with `backgroundColor` alpha 0, so the HUD background is transparent.
- `cullingMask` — what comes along into the HUD. The timer uses `Default`, `Actors` and
  `DestructibleLayers.Parts`, which brings the player and mobs standing on it into frame.
  **`Ground` is deliberately excluded**, and that is what keeps the background transparent.
- `nearClipPlane` / `farClipPlane` — see the clipping note below.
- `renderPostProcessing`, `renderShadows`, `allowHDR`, `allowMSAA` off; `renderType = Base`.

On `SceneHudElement`, set `_id` and `_resolution`. **The resolution's aspect must match the camera's
framing**, or the HUD image is stretched — the timer derives it as
`(height * framedWidth / framedDepth, height)`.

> **Clipping instead of occlusion.** Excluding `Ground` from the culling mask also removes its
> ability to occlude, so anything you expected the world to hide — for the timer, the unlit pixels
> parked below the ground plane — shows through. Cut it off with the far clip plane instead: the
> timer sets `farClipPlane` equal to the camera's height above the face, putting the far plane
> exactly on the ground plane.

### 4. Make it destructible

Per breakable part: a mesh renderer, a `Collider`, and a `DecayPart` on the `Destructable` layer
(`DestructibleLayers.Parts`). No `Rigidbody` is needed — `DestructibleObject.Impulse` adds one when
the part actually breaks off, and part colliders stay disabled until then.

Per destructible group root:

- `BoxCollider` with `isTrigger = true`, on the `Damagable` layer (`DestructibleLayers.Damagable`).
  This is both the melee damage volume and the flow-map footprint.
- `FlowMapNoGoZone` (required by `DestructibleObject`).
- `DestructibleObject` — `_breakMagnitude` is the impulse applied to the parts.
- `DestructibleHealth` — set `_objectType`.

Then add the type in two places:

- a value in [DestructibleObjectType](../Assets/Scripts/Destruction/DestructibleObjectType.cs);
- a row in `BattleBalance.DestructibleMaxHealth`. This is a **bare dictionary lookup**, so a type
  with no row throws `KeyNotFoundException` in `DestructibleHealth.Awake`.

Only `DestructibleObjectType.House` increments the buildings-destroyed statistic and takes the
ground-damage decal, so a HUD object gets its own type rather than reusing `House`.

> **Do not put a `DestructibleObject` on the root of a composite object.** If the object is made of
> several independently destructible groups (the timer's four digits and its divider), a root
> `DestructibleObject` spans all of them: `Break` iterates `GetComponentsInChildren<DecayPart>()` and
> shatters everything in one hit, bypassing the per-group logic entirely. Running
> *Tools ▸ Destruction ▸ Destructible Object Setup* on such a prefab adds exactly that — it is
> written for single-piece props. `BattleTimerObject.WarnIfRootIsDestructible` exists because this
> happened; copy that guard for any new composite.

### 5. Place it on the map

Special objects are placed freely (rotated to face the player, clearing whatever they overlap), not
on the house grid.

1. Add a value to [SpecialHouses](../Assets/Scripts/Map/SpecialHouses.cs).
2. Add a `SpecialHouseObject` entry to `HouseSet.specials` on
   `Assets/Resources/Map/HouseSet.asset` — `type`, `prefab`, `size` in cells, `enabled`.
3. Add a `SpecialSpawnRange` to `Assets/Resources/SpecialSpawnSettings.asset`:
   `initialMaxDistance` for level start, and `respawnMaxDistances[]` for each successive runtime
   respawn (the last entry repeats once the list runs out).

`MapEnvironmentSpawner.SpawnInitialSpecials` then places it automatically: the `Timer` goes down
first at its own configured distance, and every other special follows
`GetOtherSpecialPlacement` — between the timer and the player when they are far apart, otherwise in
a band near the player.

For a runtime respawn, call:

```csharp
_spawner.TrySpawnSpecial(type, anchor, lookTarget, minDistance, maxDistance, out var instance);
```

[TimerRespawnService](../Assets/Scripts/Timer/TimerRespawnService.cs) is the reference implementation
of a respawn loop.

### 6. Clean up after yourself

`EnvironmentDecayManager` destroys a destructible group's root once its parts have decayed, but
**nothing owns the composite prefab root**. If you don't destroy it, each destroyed object leaves a
husk holding a live camera and a `RenderTexture` — one more for every respawn.
`BattleTimerObject` handles this by, on death:

1. calling `BattleService.DestroyTimer()` first, so the replacement registers its HUD element;
2. breaking any group still standing, so nothing is left half-alive blocking cleanup;
3. deactivating the HUD camera GameObject;
4. destroying its own GameObject once no child carries a `DestructibleObject` any more.

## Things that bite

- **Distance culling does not apply.** `MapEnvironmentSpawner`'s `CullingGroup` is indexed by grid
  placement, and `RegisterRenderers` is only called from the grid-fill loop. Specials are never
  registered, so they are never distance-culled and stay visible at any range — verified at 990
  units. See [environment-visibility.md](environment-visibility.md); do not add specials to that
  group without a policy for keeping HUD objects exempt.
- **One camera and one render texture per live element.** They are not free. Disable the camera as
  soon as the object is dead rather than waiting for debris to decay.
- **`_resolution` aspect must match the camera framing**, or the HUD image is stretched.
- **`OnValidate` and prefab regeneration.** If the prefab is generated by an editor builder, hand
  edits are lost on the next rebuild. Put the change in the builder.

## Worked example

The battle timer prefab is fully generated by
[BattleTimerPrefabBuilder](../Assets/Scripts/Timer/Editor/BattleTimerPrefabBuilder.cs)
(*Tools ▸ Destruction ▸ Rebuild Battle Timer Prefab*), which builds all 62 boxes, the five
destructible groups and the HUD camera from layout constants at the top of the file. It is the
shortest complete read of everything above.

Generating rather than hand-authoring is worth it whenever the object is a regular grid of many
small parts: retuning pixel size or spacing is a constant edit and a rebuild, and the prefab cannot
drift from the layout the runtime code assumes.

## The compass arrow

[BattleArrowObject](../Assets/Scripts/Arrow/BattleArrowObject.cs) is the second implementation, and
the one to read if a new HUD object needs to *move*, or is built from art rather than from code.

Its prefab is generated by
[BattleArrowPrefabBuilder](../Assets/Scripts/Arrow/Editor/BattleArrowPrefabBuilder.cs)
(*Tools ▸ Destruction ▸ Rebuild Battle Arrow Prefab*) from the modelled arrow at
`Assets/Resources/Prefabs/Interface/Arrow.prefab` — three chevron segments, each mirrored in X, six
renderers in all. The builder copies that art in (a plain copy, not a nested prefab instance, so the
destruction components below are not stored as overrides on someone else's asset), turns each
segment into a breakable part, and wraps the lot in a **single** destructible group: the arrow says
one thing, so there is nothing for a half-smashed arrow to mean.

> **The generated prefab does not track the art.** Rerun the menu item after the artist changes
> `Arrow.prefab`, or the battle keeps using the old shape. Everything the builder derives — the
> trigger volume, the decay speeds, the HUD camera's height and frame — is measured from the art at
> build time, so a re-export with different proportions needs the rebuild to stay correct.

Where the timer generates a regular grid of identical boxes, this one adapts to whatever it is given:
the segments get convex `MeshCollider`s rather than boxes (a chevron's bounding box is mostly empty
air), and the camera framing comes from measured mesh bounds. `SourceYawCorrection` is the one hand-
set constant — the model as authored points along -Z, and the runtime convention is +Z.

Two things about it are not in the timer.

**The scene object never turns; its camera does.** Rolling the HUD camera about the vertical axis
rotates the image it draws, so a needle can point anywhere without dragging the object's colliders,
flow-map footprint and debris around the street with it. The camera looks straight down, so setting
its yaw to Y makes a glyph lying at world yaw Y read as pointing straight up; every degree taken off
that yaw swings the image one degree clockwise. Aiming at a bearing is therefore
`glyphYaw - bearing`, written as the camera's **world** rotation.

> **Measure the glyph's yaw, don't assume it.** `MapEnvironmentSpawner.TrySpawnSpecial` plants every
> special turned to face the player, so the prefab root — and the glyph with it — lies at whatever
> angle it was dropped at. Rolling the camera to `-bearing` as if the glyph pointed along world +Z
> puts the needle off by the root's spawn yaw, which reads as a compass that is confidently wrong.

The frame also has to be sized by the glyph's **turning circle** rather than its footprint, and the
render texture has to be square: a frame that merely fits the glyph square-on crops its corners at 45
degrees, and an oblong one crops differently depending on which way the needle happens to point.

**It is unique per battle, and not respawned.** There is one arrow, tracked by the static
`BattleArrowObject.Current` — set in `Awake`, and cleared the moment the glyph dies rather than when
the husk finally retires, so nothing is ever handed an arrow that is already on its way out. When the
timer respawns, `TimerRespawnService.MoveArrow` calls `MapEnvironmentSpawner.TryMoveSpecial` to
relocate the arrow that exists instead of spawning a second one; once it is smashed, `Current` is
null and nothing brings it back before the next battle.

`TryMoveSpecial` is the counterpart to `TrySpawnSpecial` for any object that must stay unique: it
picks a placement and clears houses out of the way exactly as a spawn does, but excludes the moving
object from that sweep (or an object whose new footprint overlapped its old one would break itself on
arrival) and rewrites its registered footprint. Skip that rewrite and the stale one keeps standing in
for it — later specials clear houses around the spot it left and land on top of it where it went.

## Why a camera instead of a data-driven HUD

The HUD deliberately has no state of its own. A widget that read a number from a service would keep
showing a tidy value while its world object lay in pieces; rendering the object itself means damage,
debris and destruction are visible for free, and the player can trust that what the HUD shows is a
thing they can walk up to and hit. The cost is one camera and one render texture per element, which
is why the lifecycle rules above — register before tear-down, disable on death, destroy the husk —
matter more than they would for an ordinary widget.
