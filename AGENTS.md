# AGENTS.md

Unity 6 (6000.3.16f1) game-jam project. URP + Input System. DI via VContainer 1.19.0.
Jam rules apply: ship the simplest thing that works. No layered abstractions "for later".

## Unity CLI

Use the `unity` CLI (`unity-cli` skill) for anything that requires Unity handling.

```bash
unity status            # is an Editor connected? look for state "ready"
unity command           # list commands this Editor exposes
unity command eval '<C#>'
unity test --format json
unity build ...
```

- `unity status` first, before touching any scene/prefab/asset.
- If connect fails, check Safe Mode: `unity pipeline list`. Safe Mode means C# compile
  errors — fix the source, restart the Editor.
- Only edit asset files directly when no Editor is reachable, and say so.
- `--format json` whenever output is parsed. Exit code 8 from `unity test` = tests failed.

## Architecture

Everything lives under `Assets/Scripts/`, namespace mirrors the folder.

### Registration

All services are registered in **one place**: `AppScope.Configure`
([Assets/Scripts/App/AppScope.cs](Assets/Scripts/App/AppScope.cs)).
`AppScope` is a persistent root `LifetimeScope` created by
[AppBootstrap](Assets/Scripts/App/AppBootstrap.cs) before the first scene loads, and disposed
on quit. Add a nested `LifetimeScope` only when a service genuinely must die with a scene.

```csharp
builder.Register<MyService>(Lifetime.Singleton)
  .AsSelf()                    // needed if others resolve/inject it by concrete type
  .AsImplementedInterfaces();  // needed for the lifetime interfaces below to fire
```

- `Lifetime.Singleton` by default. `Transient` only for genuinely per-use objects.
- Register the concrete type; introduce an interface only when there is a second implementation.

### Lifetime interfaces (VContainer)

Implemented interfaces are only invoked when the registration includes
`.AsImplementedInterfaces()` (or `RegisterEntryPoint<T>()`).

- `IInitializable` — after container build. Do setup here, not in the constructor.
- `IStartable` / `IPostStartable` — first frame.
- `ITickable` / `IFixedTickable` / `ILateTickable` — per-frame updates; drop them if unused.
- `IDisposable` — teardown. **Unsubscribe from every event here.**
- `IAsyncStartable` — async startup.

### Events

Plain C# `event`s declared public on the service that owns them. No event bus, no messaging
service.

```csharp
public class ScoreService
{
  public event Action<int> ScoreChanged;
}

public class HudService : IInitializable, IDisposable
{
  private readonly ScoreService _score;
  public HudService(ScoreService score) => _score = score;

  void IInitializable.Initialize() => _score.ScoreChanged += OnScoreChanged;
  void IDisposable.Dispose() => _score.ScoreChanged -= OnScoreChanged;
}
```

Subscribing without a matching unsubscribe in `Dispose` is a bug.

For `MonoBehaviour` UI components, listener lifetime follows enabled state:

- Inject dependencies in `Awake`.
- Add button and event listeners in `OnEnable`, then refresh the displayed state there.
- Remove those same listeners in `OnDisable`.
- Do not use `Awake`/`OnDestroy` as the subscription lifetime for UI listeners.

### Injecting outside the container

Objects the container does not create — `MonoBehaviour`s, plain `new`-ed classes — pull
their dependencies with `.AsInjected()` from
[DiExtensions](Assets/Scripts/App/DiExtensions.cs): in `Awake` for a `MonoBehaviour`, in the
constructor for a regular class. Fields are marked `[Inject]`.

```csharp
public class Player : MonoBehaviour
{
  [Inject] private ScoreService _score;
  private void Awake() => this.AsInjected();
}
```

`DiExtensions.Resolve<T>()` exists for one-off lookups — prefer `[Inject]` + `AsInjected()`.

### Reference

`Library/PackageCache/jp.hadashikick.vcontainer*/` holds the VContainer source — read it
instead of guessing the API.
