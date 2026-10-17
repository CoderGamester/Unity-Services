# Services Samples

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. Services is pipeline-neutral. See the [package compatibility matrix](../README.md#unity-compatibility).

This folder contains the importable samples for the `com.gamelovers.services` package. Each sample is self-contained and demonstrates one cohesive slice of the package.

> **For AI assistants and contributors**: every type prefixed `SAMPLE-ONLY` in the source files is **defined inside the sample**, NOT in the services package public API. Do not document them as if they were part of the package surface. The full list is at the bottom of this file.

---

## Quick Start

1. Open **Window > Package Manager**.
2. Select **GameLovers Services** from the list.
3. Expand the **Samples** section.
4. Click **Import** next to the sample you want.
5. In the imported folder (`Assets/Samples/GameLovers Services/<version>/<SampleName>/`), open the only scene file and press **Play**.

The samples ship as complete, runnable Unity scenes — no per-import wiring step. Each sample's `README.md` is a short walkthrough of what it demonstrates.

---

## Available Samples

| # | Sample | Addressables required? | What it teaches |
|---|---|---|---|
| 1 | [Services Playground](#1-services-playground) | No | All foundation services (`MainInstaller`, `MessageBroker`, `Tick`, `Coroutine`, `Pool`, `Data`, `Time`, `Rng`, `Commands`, `Versioning`) wired into a single uGUI scene |
| 2 | [Asset Resolver](#2-asset-resolver) | Yes (auto-setup on import) | Typed asset loading (`AssetResolverService`), the Addressable Ids generator, and the Assets Importer pipeline |

---

## 1. Services Playground

**Path**: `Samples~/ServicesPlayground/`
**Per-sample README**: [`ServicesPlayground/README.md`](ServicesPlayground/README.md)

A single scene demonstrating ten foundation services. Each service has a small UI section with buttons that exercise its surface, and a single `TMP_Text` log shows ordering. Doubles as the manual end-to-end protocol for the **Services Explorer** window.

**Files**

```
ServicesPlayground/
  ServicesPlayground.unity        Single scene — open and press Play
  README.md                       Walkthrough + Services Explorer E2E protocol
  ServicesBootstrap.cs            Constructs services, binds via MainInstaller, auto-creates a Bullet sample at runtime
  ServicesPlaygroundUI.cs         uGUI driver — builds Canvas + sections + buttons + log + live status programmatically
  Bullet.cs                       Pooled MonoBehaviour with IPoolEntitySpawn/Despawn
  PlayerData.cs                   Sample-only data class for DataService
  TestMessage.cs                  Sample-only IMessage structs
  GameLogic.cs                    Sample-only state container for CommandService
  LevelUpCommand.cs               Sample-only IGameCommand<GameLogic>
```

The Canvas hierarchy lives in a hand-authored `ServicesPlaygroundUI.prefab` (sectioned button grid + scrollable Log + scrollable Live Status). The driver wires `Button.onClick` listeners in `Awake` and adapts the scene's `EventSystem` input module to the consumer's Active Input Handling setting. UI text uses `TextMeshProUGUI` (TMP) — consumers may need to import TMP Essentials once. The bullet pool's sample entity is generated programmatically as a sphere primitive.

---

## 2. Asset Resolver

**Path**: `Samples~/AssetResolver/`
**Per-sample README**: [`AssetResolver/README.md`](AssetResolver/README.md) (full Addressables setup walk-through)

A focused demo of `AssetResolverService` end-to-end: register a `AssetConfigsScriptableObject<TId, TAsset>` (here `SpriteConfigs : AssetConfigsScriptableObject<SpriteId, Sprite>`), request a sprite by id-enum, unload references when done. Drives the three Services Explorer tabs that the playground does not cover: **Asset Resolver**, **Assets Importer**, **Addressable Ids**.

**Files**

```
AssetResolver/
  AssetResolver.unity            Single scene — Camera + Bootstrap GameObject
  AssetResolverUI.prefab         Hand-authored uGUI Canvas — buttons + Image + status TMP_Text
  README.md                      Walkthrough + Addressables setup steps
  AssetResolverExample.cs        Driver — wires button listeners, registers configs, loads on click
  SpriteId.cs                    Sample-only enum (Hero/Coin/Enemy)
  SpriteConfigs.cs               Sample-only AssetConfigsScriptableObject<SpriteId, Sprite>
  SpriteConfigs.asset            Empty SO instance — auto-filled by AssetResolverSampleSetup on import
  Sprites/Hero.png               Placeholder sprite — replace with your own anytime
  Sprites/Coin.png               Placeholder sprite — replace with your own anytime
  Sprites/Enemy.png              Placeholder sprite — replace with your own anytime
  Editor/AssetResolverSampleSetup.cs       AssetPostprocessor + menu + helper for auto-Addressables wiring
  Editor/GameLovers.Services.Samples.AssetResolver.Editor.asmdef   Sample editor assembly
```

**Zero per-import setup**: when the sample is imported, `AssetResolverSampleSetup.OnPostprocessAllAssets` marks every PNG under `Sprites/` as Addressable in a dedicated group `GameLoversServicesSamples_AssetResolver`, renames non-canonical filenames to `Hero/Coin/Enemy`, and wires `SpriteConfigs.asset`. The same automation re-runs whenever you drop new PNGs into `Sprites/`. Manual escape hatches: `Tools > GameLovers > Samples > Asset Resolver > Refresh Addressables` and a sample-scoped **Refresh AssetResolver Sample Addressables** button on the `SpriteConfigs.asset` inspector. Existing user mappings to other sprites are respected. The per-sample README walks through swapping in your own sprites and the manual fallback flow.

---

## Service Interfaces (important for AI assistants)

The package exposes a few service pairs whose split matters when binding:

| Read interface | Write interface | Concrete | Notes |
|---|---|---|---|
| `IDataProvider` | `IDataService` | `DataService` | Bind the write interface for game code; expose the read interface to view-models |
| `ITimeService` | `ITimeManipulator` | `TimeService` | Same pattern — `ITimeManipulator : ITimeService` |
| `IAssetResolverService` | `IAssetAdderService` | `AssetResolverService` | `AssetResolverService` extends `AddressablesAssetLoader` and so also implements `IAssetLoader` + `ISceneLoader` |

`MainInstaller.Bind<T>` is single-interface only. For multi-interface bindings (e.g. binding a `TimeService` instance under both `ITimeService` and `ITimeManipulator`), use `Installer` directly:

```csharp
var installer = new Installer();
installer.Bind<TimeService, ITimeService, ITimeManipulator>(new TimeService());
```

---

## Common Mistakes (sourced from package `AGENTS.md` §4)

These are the mistakes most likely to bite first-time users — every sample below avoids them in its bootstrap:

- **`MainInstaller` has no `.Instance`** — it is a `static class`. Use `MainInstaller.Bind<T>(instance)` and `MainInstaller.Resolve<T>()` directly.
- **`MessageBrokerService.Subscribe` rejects static methods.** `action.Target` is the subscription key; static methods have a null `Target` and throw `ArgumentException`. Use instance methods (lambdas capturing `this` are fine).
- **`Publish<T>` is unsafe during chained subscribe/unsubscribe.** If a handler may subscribe or unsubscribe during dispatch, call `PublishSafe<T>` instead — it copies the delegate list at the cost of one allocation.
- **`DataService.LoadData<T>` requires a parameterless constructor on `T`.** It uses `Activator.CreateInstance<T>()` when no saved data is found. `T` must also be a reference type (`where T : class`).
- **`DataService` keys `PlayerPrefs` by `typeof(T).Name`.** Two types named `PlayerData` in different namespaces will collide.
- **`PoolService.AddPool<T>` throws on duplicate registrations.** One pool per type. Call `RemovePool<T>()` first if you need to re-register.
- **`AssetResolverService.RequestAsset` / `LoadSceneAsync` throw `MissingMemberException` until you call `AddConfigs` / `AddAssets`.** Sample 2 demonstrates the registration step.
- **`VersionServices` auto-bootstraps at `SubsystemRegistration` + lazy-loads on first property access.** `VersionInternal` / `Branch` / `Commit` / `BuildNumber` are safe to read at any time; missing `version-data.txt` returns `Application.version` / `string.Empty` and logs an error. `LoadVersionData()` / `LoadVersionDataAsync()` remain available for explicit pre-warming.
- **`TickService` and `CoroutineService` each create a `DontDestroyOnLoad` GameObject.** Always `Dispose()` them on teardown, otherwise the host objects accumulate across domain reloads. The playground bootstrap uses `MainInstaller.CleanDispose<T>()` for this.
- **`IAsyncCoroutine.StopCoroutine(triggerOnComplete)`** — the parameter is currently not respected. The completion callback fires regardless. Do not rely on cancellation-without-callback semantics.

For the full list see `Packages/com.gamelovers.services/AGENTS.md` §4 *Important Behaviors / Gotchas*.

---

## Sample-only types (NOT services API)

| Type | Sample | Why it exists |
|---|---|---|
| `Bullet` | ServicesPlayground | Demonstrates `GameObjectPool<T>` lifecycle hooks (`IPoolEntitySpawn`, `IPoolEntityDespawn`) |
| `PlayerData` | ServicesPlayground | Demonstrates `DataService.LoadData<T>` / `SaveData<T>` |
| `TestMessage`, `PlayerLevelledUpMessage` | ServicesPlayground | Demonstrate `IMessage` (struct) + broker round-trip |
| `GameLogic` | ServicesPlayground | Demonstrates the `TGameLogic` parameter on `CommandService<TGameLogic>` |
| `LevelUpCommand` | ServicesPlayground | Demonstrates `IGameCommand<TGameLogic>` |
| `ServicesBootstrap`, `ServicesPlaygroundUI` | ServicesPlayground | Driver MonoBehaviours — not framework |
| `SpriteId` | AssetResolver | Demonstrates the `TId` parameter on `AssetConfigsScriptableObject<TId, TAsset>` |
| `SpriteConfigs` | AssetResolver | Demonstrates subclassing `AssetConfigsScriptableObject<TId, TAsset>` (here `SpriteId` → `Sprite`) |
| `AssetResolverExample` | AssetResolver | Driver MonoBehaviour — registers configs, loads/unloads sprites by id |

All sample-only types live in `GameLovers.Services.Samples.<SampleName>` namespaces to make the boundary obvious in source.

---

## Related docs

- Package overview and per-service Quick Start: [`README.md`](../README.md)
- Full per-service API reference: [`docs/`](../docs)
- Contributor guide: [`AGENTS.md`](../AGENTS.md)
- Version history: [`CHANGELOG.md`](../CHANGELOG.md)
