# Environment Object Decay

## Goal

Design a simple runtime system for destructible environment objects that can:

- Spawn objects on a grid.
- Decay destroyed objects and individual object parts.
- Make destroyed parts fall through the ground at authored, per-part speeds.
- Remove parts when they are completely below ground.
- Remove the whole object after all of its visible parts are buried.

No implementation changes are part of this design step.

## Suggested approach

Separate placement, destruction state, and decay processing.

```text
LevelEditor
├── map authoring and grid visualization
└── editor preview commands

MapEnvironmentSpawner
├── calls MapFiller
├── instantiates environment prefabs
├── owns the runtime container
└── tracks spawned objects and runtime occupancy

EnvironmentDecayManager
└── centrally updates falling parts and removes buried objects
```

### Grid spawning

Keep `MapFiller` as the pure placement algorithm. `MapData` describes authored
filled cells, while runtime occupancy is tracked separately.

Each house/object definition provides its prefab and grid footprint. The runtime
spawner checks and reserves the complete footprint before instantiating an object.
Each spawned object receives a runtime identity, grid footprint, and reference to
its authored parts.

Do not modify `MapData.FilledCells` when an object is destroyed; it is map input,
not runtime state.

### Object and part lifecycle

Object lifecycle:

```text
Intact -> Destroying -> Buried -> Removed
```

Part lifecycle:

```text
Intact -> Falling -> Buried -> Removed
```

Destruction should immediately disable gameplay colliders, preserve the visual
temporarily, and register destroyed parts with the decay manager. Partial
destruction schedules only the affected parts; whole-object destruction schedules
all parts.

Each prefab part stores its own decay settings, such as destruction delay, fall
speed, sink depth, and optionally an easing curve. This avoids hard-coded behavior
in the manager and supports different behavior for different parts of an object.

Use pre-authored child parts rather than spawning fragment prefabs unless
independent physics is genuinely required.

### Decay and burial

Use one central decay manager instead of an `Update` method on every object or
part. It processes elapsed time from each part's destruction timestamp and moves
the part downward using its authored speed.

At decay start, query the ground below the part using the ground layer. A part is
buried when its visible bounds, or an authored burial probe, are completely below
the sampled ground height plus the configured sink depth. Do not rely only on the
part pivot or object root position.

The object is removed only after all of its visible parts are buried or removed.
For the initial jam implementation, ordinary `Destroy` is sufficient; introduce
pooling only if profiling shows repeated spawning and removal needs it.

### Existing script boundaries

`MapFiller` can remain mostly unchanged. `LevelEditor` should eventually stop
owning runtime instantiation, the runtime container, and runtime cleanup. Its
`Fill` and `Clear` operations can remain as editor-preview commands or delegate to
a preview spawner.

`ImpulseDestructible` is the current destruction trigger. There are two possible
directions:

1. For deterministic authored fall speeds, change it to notify the destructible
   object/decay manager instead of making all child rigidbodies dynamic.
2. For physics-driven debris, keep its Rigidbody behavior and let the decay
   manager only detect burial. This will not guarantee exact per-part fall speeds.

The first option better matches the stated requirement for prebaked per-part
speeds.

### Occupancy policy

Initially, keep grid cells occupied until the object is fully removed. This avoids
overlapping a new object with an object that is still visually decaying. If
respawning is added later, track gameplay occupancy and visual occupancy as
separate states so they can be released at different times.
