# Migration Guide — v1.x → v2.0.0

## Overview

v2.0.0 absorbs `com.gamelovers.assetsimporter` into `com.gamelovers.services`. The package now has two new hard dependencies (`com.unity.addressables`, `com.cysharp.unitask`) and a reorganized `Runtime/` and `Editor/` folder structure.

**Namespace changes in 2.0.0** affect pool interfaces, command interfaces, asset importer types, and editor versioning code. The concrete service classes (`PoolService`, `CommandService<>`, etc.) and foundation services (DI, messaging, ticking, persistence, time, RNG, versioning) stay in `GameLovers.Services`.

Consumers should update their `using` directives per sections 2, 3, and below.

---

## 1. Package Reference

Remove `com.gamelovers.assetsimporter` and update `com.gamelovers.services` in your `manifest.json` or submodule:

```diff
- "com.gamelovers.assetsimporter": "https://github.com/CoderGamester/Unity-AssetsImporter.git",
  "com.gamelovers.services": "https://github.com/CoderGamester/Unity-Services.git#2.1.2",
```

`com.unity.addressables` resolves from the Unity registry. When installing from Git, add UniTask directly to the consuming project's manifest; Unity does not resolve Git dependencies transitively.

---

## 2. Pool Types (Breaking)

Pool interfaces and pool implementation types moved from `GameLovers.Services` to `GameLovers.Services.Pooling`.

```diff
- using GameLovers.Services;
+ using GameLovers.Services;
+ using GameLovers.Services.Pooling;
```

Affected types moved to `GameLovers.Services.Pooling`: `IPoolService`, `IObjectPool`, `IObjectPool<T>`, `IPoolEntitySpawn`, `IPoolEntitySpawn<T>`, `IPoolEntityDespawn`, `IPoolEntityObject<T>`, `ObjectPoolBase<T>`, `ObjectPool<T>`, `GameObjectPool`, `GameObjectPool<T>`.

`PoolService` (concrete) stays in `GameLovers.Services`.

## 3. Command Types (Breaking)

Command interfaces moved from `GameLovers.Services` to `GameLovers.Services.Commands`.

```diff
- using GameLovers.Services;
+ using GameLovers.Services;
+ using GameLovers.Services.Commands;
```

Affected types moved to `GameLovers.Services.Commands`: `IGameCommandBase`, `IGameCommand<>`, `IGameServerCommand<>`, `ICommandService<>`.

`CommandService<>` (concrete) stays in `GameLovers.Services`.

## 4. Asset Importer Runtime Types

All types previously in `GameLovers.AssetsImporter` (except `AssetResolverService`) are now in `GameLovers.Services.AssetsImporter`.

```diff
- using GameLovers.AssetsImporter;
+ using GameLovers.Services.AssetsImporter;
```

Affected types: `IAssetLoader`, `ISceneLoader`, `AddressablesAssetLoader`, `AddressableConfig`, `AddressableConfigComparer`, `AssetConfigsScriptableObject<TId,TAsset>`, `AssetConfigsScriptableObjectBase<TId,TAsset>`, `AssetLoaderUtils`, `AssetReferenceScene`.

---

## 5. AssetResolverService

`AssetResolverService` (and `IAssetResolverService` / `IAssetAdderService`) moved from `GameLovers.AssetsImporter` to `GameLovers.Services` (the root services namespace).

```diff
- using GameLovers.AssetsImporter;
- // AssetResolverService was in GameLovers.AssetsImporter
+ using GameLovers.Services;
+ // AssetResolverService is now in GameLovers.Services
```

If you only used `using GameLovers.AssetsImporter;` for `AssetResolverService` and nothing else, replace it with `using GameLovers.Services;` (which you almost certainly already have).

---

## 6. Editor Versioning Code

`VersionEditorUtils` and `GitEditorProcess` moved from namespace `GameLovers.Services.Editor` to `GameLovers.Services.Versioning.Editor`.

```diff
- using GameLovers.Services.Editor;
+ using GameLovers.Services.Versioning.Editor;
```

---

## 7. Asset Importer Editor Types

Editor types previously in `GameLoversEditor.AssetsImporter` are now in `GameLovers.Services.AssetsImporter.Editor`.

```diff
- using GameLoversEditor.AssetsImporter;
+ using GameLovers.Services.AssetsImporter.Editor;
```

Affected types: `AssetsImporter`, `AssetsToolImporter`, `IAssetConfigsImporter`, `AssetsConfigsImporter<>`, `AssetsConfigsImporterBase<>`, `AssetsConfigsGeneratorImporter<>`, `IAssetConfigsGeneratorImporter`, `AddressablesIdGeneratorSettingsEditor`, `AddressablesIdGeneratorSettings`.

---

## 8. Re-run Code Generators

If you previously used `AddressableIdsGenerator` (Tools → AddressableIds Generator), the generated file contains a `using` statement that must be updated.

**Option A — Re-run the generator**: Open Unity, go to Tools → AddressableIds Generator → Generate AddressableIds. The newly generated file will have the correct `using GameLovers.Services.AssetsImporter;`.

**Option B — Manual fix** in the generated file:

```diff
- using GameLovers.AssetsImporter;
+ using GameLovers.Services.AssetsImporter;
```

Similarly, if you used `AssetsConfigsGeneratorImporter<TAsset>` to code-generate an importer, re-run by setting the folder path on the importer ScriptableObject, or fix the generated file manually with the same `using` replacement.

---

## 9. `IAssetLoader.UnloadAsset` Signature Change

`IAssetLoader.UnloadAssetAsync<T>(T, Action)` was renamed to `IAssetLoader.UnloadAsset<T>(T, Action)` and its return type changed from `UniTask` to `void`. The underlying behaviour (`Addressables.Release(asset)` + invoke callback) is already synchronous, so the previous `UniTask`-returning signature was misleading; the rename makes the API honest.

```diff
- await loader.UnloadAssetAsync(texture);
+ loader.UnloadAsset(texture);
```

If you chained the release inside an `async` method, the fix is a single-line change: drop the `await` and the `Async` suffix. No other call-site adjustments are required.

**Behavioural reminder**: `UnloadAsset` only decrements the Addressables reference count. It does not free memory and does not destroy `GameObject` instances returned by `InstantiateAsync`. To reclaim memory, call `UnityEngine.Resources.UnloadUnusedAssets()` at scene transitions, boot, or memory-pressure events. To destroy instantiated prefabs, call `UnityEngine.Object.Destroy` separately.

---

## 10. What Did NOT Change

The following types remain in namespace `GameLovers.Services` — no action needed if you only use `using GameLovers.Services;`:

- DI: `IInstaller`, `Installer`, `MainInstaller`
- Messaging: `IMessageBrokerService`, `MessageBrokerService`, `IMessage`
- Ticking: `ITickService`, `TickService`
- Coroutines: `ICoroutineService`, `CoroutineService`, `IAsyncCoroutine`, `IAsyncCoroutine<T>`
- Data: `IDataService`, `IDataProvider`, `DataService`
- Time: `ITimeService`, `ITimeManipulator`, `TimeService`
- RNG: `IRngService`, `RngService`, `RngData`, `IRngData`
- Versioning: `VersionServices`, `VersionData`
- Concrete services and their interfaces that remain in `GameLovers.Services`: `PoolService`, `CommandService<>`, `AssetResolverService`, `IAssetResolverService`, `IAssetAdderService`

The following types **moved** to sub-namespaces (see sections 2, 3, 4 above):

- `GameLovers.Services.Pooling`: `IPoolService`, `IObjectPool`, `IObjectPool<T>`, `IPoolEntitySpawn`, `IPoolEntitySpawn<T>`, `IPoolEntityDespawn`, `IPoolEntityObject<T>`, `ObjectPoolBase<T>`, `ObjectPool<T>`, `GameObjectPool`, `GameObjectPool<T>`
- `GameLovers.Services.Commands`: `IGameCommandBase`, `IGameCommand<>`, `IGameServerCommand<>`, `ICommandService<>`
- `GameLovers.Services.AssetsImporter`: `IAssetLoader`, `ISceneLoader`, `AddressablesAssetLoader`, `AddressableConfig`, `AddressableConfigComparer`, `AssetConfigsScriptableObject`, `AssetConfigsScriptableObjectBase<,>`, `AssetConfigsScriptableObject<,>`, `AssetLoaderUtils`, `AssetReferenceScene`
