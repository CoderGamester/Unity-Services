# GameLovers Services — Agent Guide

This guide adds package-specific rules to the host repository guide. Consumer usage belongs in `README.md` and `docs/`.

## Scope

- Package: `com.gamelovers.services`; minimum Unity version and dependencies are authoritative in `package.json`.
- Runtime includes installation/service lookup, messaging, ticking, coroutines, pooling, persistence, time, deterministic RNG, commands, version metadata, and Addressables-backed asset resolution.
- Editor tooling includes the Services Explorer, asset/config importers, Addressable ID generation, scaffolders, and version-data generation.
- This package is render-pipeline-neutral.

## Namespace and assembly boundaries

- Concrete services at `Runtime/` root use `GameLovers.Services`.
- `Runtime/DependencyInjection/` is a namespace carve-out and also uses `GameLovers.Services`.
- Contracts and implementations under `Runtime/Commands/`, `Runtime/Pooling/`, and `Runtime/AssetsImporter/` use their corresponding child namespaces.
- Runtime must not reference `UnityEditor`. Editor introspection uses `internal` read-only accessors plus `InternalsVisibleTo`; do not widen runtime APIs solely for the Services Explorer.

## Runtime invariants

- `Installer` binds interface types to instances. Non-interface and duplicate bindings throw. `MainInstaller` exposes only single-interface binding; multi-interface binding is on `Installer`/`IInstaller`.
- `Publish<T>` iterates the live subscriber collection and cannot tolerate subscription mutation. Use `PublishSafe<T>` when handlers may subscribe or unsubscribe. Static-method subscriptions are unsupported because subscribers are keyed by `action.Target`.
- `Unsubscribe<T>(null)` clears subscribers for that message type; `UnsubscribeAll(null)` clears all message types. Preserve these explicit bulk-clear contracts.
- `TickService` and `CoroutineService` each own a `DontDestroyOnLoad` host and are not singletons. Dispose every created instance.
- Public async-coroutine completion callbacks have replace semantics. Editor tracking uses the separate internal cleanup event and must not overwrite consumer callbacks.
- `PoolService` permits one pool per entity type. Pool code that touches Unity objects must guard Unity fake-null because consumers may destroy pooled objects externally.
- `ObjectPool<T>` dispatches lifecycle hooks by casting the entity. `GameObjectPool` and `GameObjectPool<TBehaviour>` discover hooks with `GetComponent`; implement hooks on the object appropriate to the chosen pool type.
- `DataService` stores reference types by `Type` and persists them through one `IDataStorage` (`PlayerPrefsDataStorage` by default). Storage keys are `typeof(T).Name`, prefixed `{keyPrefix}.` only when a key prefix is given, so the default keys stay compatible with existing saves. Absent saved values require a parameterless constructor.
- `BrowserLocalDataStorage` keeps its `[DllImport]` declarations behind `UNITY_WEBGL && !UNITY_EDITOR`; elsewhere its construction throws `PlatformNotSupportedException`. Never make it fall back silently.
- `AddressablesAssetLoader.UnloadAsset` releases synchronously and does not destroy instantiated GameObjects or force global memory collection.
- `IAssetAdderService.AddConfigs` is a default interface method and dispatches through an interface-typed reference, not through `AssetResolverService` directly.
- `AssetConfigsScriptableObject<TId,TAsset>` stores `AssetReference` entries through its base class; `TAsset` describes the resolved asset type. Do not replace the weak-link storage with serialized concrete assets.
- `VersionServices` auto-loads and lazy-loads `version-data`; missing data returns documented fallbacks and logs an error. `IsOutdatedVersion` requires three-part semantic versions unless the method is hardened.
- `CommandService<TGameLogic>` exposes protected `GameLogic` and `MessageBroker` for subclasses, but `ExecuteCommand` is not virtual. Interception requires an intentional interface implementation or explicit shadowing.

## Editor invariants

- Settings derived from `ScriptableSingleton` live under `ProjectSettings/`. Include `using UnityEngine;` when using `SerializeField`.
- Addressable ID generation records its last successful sorted address/label snapshot in `AddressableIdsEditorSettings.asset`. That project-shared baseline is rewritten by generation and should not be silently ignored or recomputed during every Explorer refresh.
- Services Explorer tab navigation is typed to `ServiceTab`; callers pass tab types, never service interfaces.
- Tabs that rebuild foldouts on refresh must preserve expansion state and short-circuit unchanged refreshes so periodic rebuilds do not lose pointer input. Filter nested foldout change events by target.
- Runtime-state tabs clear synchronously when leaving Play mode and also reject populated refreshes in Edit mode. Do not depend on consumer teardown to clean the display.
- Keep expensive Addressable ID diffs user-triggered; periodic refresh may use only cheap freshness checks.

## Samples and tests

- `ServicesPlayground` remains free of Addressables/Resources setup and uses the shipped TMP-backed prefab UI.
- `AssetResolver` owns its sample-scoped Addressables automation, runtime/editor asmdefs, group, and label. Do not move sample automation into the package Editor assembly.
- The AssetResolver sample binds its real resolver through `MainInstaller` so the Explorer sees the running instance. Crosses into Editor code through sample-scoped menu commands, not runtime assembly references.
- Sample automation is idempotent and preserves existing non-empty consumer mappings. Manual cleanup of the sample-owned Addressables group/label remains documented because a deleted postprocessor cannot observe its own removal.
- Sample scenes/prefabs and scripts use deterministic file-level `.meta` GUIDs so serialized script references survive a fresh Package Manager import. Preserve or deliberately update those GUID relationships when changing sample assets.
- Before changing anything under `Tests/`, read `Tests/AGENTS.md`.

## Verification and documentation

- Lifecycle changes require ownership, idempotent disposal, and multi-instance isolation tests.
- Addressables/importer changes require explicit catalog or `AssetDatabase` verification in addition to unit tests.
- Update `README.md`/`docs/` for consumer behavior and `CHANGELOG.md` for notable changes. Update the sample index, per-sample README, and `package.json` together when sample structure changes.
- Update this guide only for durable package invariants, boundaries, or test conventions.
