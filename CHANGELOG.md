# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Unreleased]

**Changed**:
- Lowered the declared Unity minimum from 6000.3 to 6000.0 and documented 6000.0.x, 6000.3.x, and 6000.5.x as compatibility reference streams.

**Fixed**:
- The AssetResolver sample now assigns default actions to the Input System UI module it adds at runtime, so its buttons respond to input instead of looking interactive and receiving nothing.

## [2.1.2] - 2026-08-04

**Changed**:
- Added the `com.unity.test-framework.performance` (3.5.0) dependency so the package's test assemblies compile when tests are enabled.

**Fixed**:
- Fixed `GameObjectPool.Dispose(false)` and its generic equivalent so they preserve the sample entity when requested.
- Fixed pooling of Unity objects so destroyed pooled `GameObject` and `Behaviour` instances are skipped instead of being returned to callers.
- Fixed Addressable ID enum generation so addresses that sanitize to the same C# identifier receive distinct members.
- Fixed the Services Playground Input System setup by assigning default actions when its UI module is created, so sample controls respond as expected.

## [2.1.1] - 2026-07-04

**Changed**:
- PlayMode tests updated to parameterless `FindObjectsByType<T>()` overload (Unity 6 API).
- Removed redundant `[Serializable]` from `AddressableConfig` (already implicitly serializable as a reference type in Unity YAML).

**Fixed**:
- `ServicesScaffolders` adapts to Unity 6000.4+ `AssetCreationEndAction` / `EntityId` API (guarded by `UNITY_6000_4_OR_NEWER`; pre-6000.4 path unchanged).

## [2.1.0] - 2026-05-20

**New**:
- `VersionServices` now auto-bootstraps via `[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]`, populating version metadata before any scene `Awake` callback and before vendor-SDK `SubsystemRegistration` callbacks that read it. Consumers no longer need to call `LoadVersionData()` / `LoadVersionDataAsync()` explicitly for the default flow.

**Changed**:
- Property getters (`VersionInternal`, `Branch`, `Commit`, `BuildNumber`) now lazy-load via a new private `EnsureLoaded()` on first access if the auto-bootstrap hook has not yet fired — protects against undefined ordering between sibling assemblies' `[RuntimeInitializeOnLoadMethod]` callbacks at the same phase.
- Removed the private `IsLoaded()` helper (replaced by `EnsureLoaded()` invoked from each property getter).

**Docs**:
- `docs/version-services.md` rewritten around the new auto-bootstrap contract: the recommended usage no longer includes any explicit load call, the lazy-load fallback is documented, and the Error Reference table now describes the fallback behaviour (no exception is raised).

## [2.0.2] - 2026-05-20

**Fixed**:
- Add missing meta file

## [2.0.1] - 2026-05-20

**New**
- Added new test suite for more rebust code coverage
- Added `VersionServices.LoadVersionData()` — synchronous sibling of `LoadVersionDataAsync()` for consumers who want to populate version metadata at boot without an `await`. Both methods now funnel into a shared private `ApplyTextAsset` helper, so behaviour is identical. Sync is the recommended default for the shipping `version-data.txt` (a few hundred bytes); async remains available for cases where `VersionData` is extended with large embedded blobs. Covered by `Tests/EditMode/Unit/VersionServicesSyncLoadTest.cs`.

**Fixed**:
- `Tests/AGENTS.md` §8 extended with a new **"Authorized reflection sites (storage-assertion exception)"** subsection.

## [2.0.0] - 2026-04-26

**New**:
- Added **Services Explorer** window (`Tools > GameLovers > Services Explorer`) with 13 live-refresh tabs: Overview, Installer, MessageBroker, Tick, Coroutine, Pool, Data, Time, RNG, AssetResolver, Versioning, Assets Importer, Addressable Ids — works in both Edit and Play mode
- Menu stubs under `Tools > GameLovers`:
  - `Versioning / Refresh Version Data` and `Versioning / Open in Explorer`
  - `Assets Importer / Import Assets Data` and `Assets Importer / Open in Explorer`
  - `Addressable Ids / Generate Addressable Ids` and `Addressable Ids / Open in Explorer`
- Added `Assets > Create > GameLovers Services > …` scaffolders: Message, Command, Service, Pool Entity (template-based, $NAME$ / $NAMESPACE$ substitution)
- Absorbed `com.gamelovers.assetsimporter` v0.5.2 into this package
- Added `IAssetLoader`, `ISceneLoader`, `AddressablesAssetLoader` to `Runtime/AssetsImporter/` (ns `GameLovers.Services.AssetsImporter`)
- Added `AddressableConfig`, `AssetConfigsScriptableObject<TId,TAsset>`, `AssetLoaderUtils`, `AssetReferenceScene` to `Runtime/AssetsImporter/` (ns `GameLovers.Services.AssetsImporter`)
- Added `AssetResolverService` (implements `IAssetResolverService` / `IAssetAdderService`) to `Runtime/` root (ns `GameLovers.Services`)
- Added Editor/AssetsImporter/: `AssetsImporter`, `AssetsToolImporter`, `AssetConfigsImporter`, `AddressableIdsGenerator`, `AddressablesIdGeneratorSettings` (ns `GameLovers.Services.AssetsImporter.Editor`)
- Added importable **Samples** under `Samples~/` (importable via Unity Package Manager > GameLovers Services > Samples).
  - **Services Playground** — single-scene, zero-setup walk-through that wires every foundation service via `MainInstaller` and exercises 10 of 13 Services Explorer tabs end-to-end.
  - **Asset Resolver** — focused demo of `AssetResolverService` end-to-end (`AddConfigs` / `RequestAsset` / `UnloadAssets`) with `SpriteConfigs : AssetConfigsScriptableObject<SpriteId, Sprite>`. Drives the three Services Explorer tabs the Playground does not cover (Asset Resolver, Assets Importer, Addressable Ids).

**Changed**:
- Addressable Ids generator and Assets Importer settings moved from `Assets/*.asset` ScriptableObjects to `ProjectSettings/` ScriptableSingletons (mirrors `VersioningEditorSettings`).
- Generation logic extracted from `AddressableIdsGenerator` into `AddressableIdsGeneratorUtils` (static `internal`); importer discovery/import logic extracted into `AssetsImporterEditorUtils`.
- `Tools/Assets Importer/*` and `Tools/AddressableIds Generator/*` menu entries removed. Use `Tools/GameLovers/Assets Importer/...`, `Tools/GameLovers/Addressable Ids/...`, or the Services Explorer tabs instead.
- `Toggle Auto Import On Refresh` menu entry removed. The toggle now lives exclusively in the Services Explorer **Assets Importer** tab.
- `Assets/AssetsImporter.asset` and `Assets/AddressablesIdGeneratorSettings.asset` are no longer used (settings moved to `ProjectSettings/`); safe to delete from consumer projects.
- Deleted editor source files: `AssetsImporter.cs`, `AssetsToolImporter.cs`, `AddressableIdsGenerator.cs`, `AddressablesIdGeneratorSettings.cs`.
- Folder reorganization: `Runtime/` now has domain subfolders `DependencyInjection/`, `Commands/`, `Pooling/`, `AssetsImporter/`; `Editor/` now has `Versioning/` and `AssetsImporter/` subfolders
- `Installer.cs` and `MainInstaller.cs` moved to `Runtime/DependencyInjection/` (namespace unchanged: `GameLovers.Services`)
- `CommandService.cs` trimmed to concrete class only; command contract interfaces extracted to `Runtime/Commands/` under ns `GameLovers.Services.Commands`
- `PoolService.cs` trimmed to concrete class only; pool interfaces + implementations moved to `Runtime/Pooling/` under ns `GameLovers.Services.Pooling`
- `ObjectPool.cs` (578 lines, 10 types) split into 4 files under `Runtime/Pooling/`: `IPoolEntity.cs`, `IObjectPool.cs`, `ObjectPool.cs`, `GameObjectPool.cs`
- `VersionEditorUtils.cs` and `GitEditorProcess.cs` moved to `Editor/Versioning/`; re-namespaced from `GameLovers.Services.Editor` → `GameLovers.Services.Versioning.Editor`
- Added new hard dependencies: `com.unity.addressables` (1.21.20) and `com.cysharp.unitask` (2.5.10)

**Fixed**:
- `AddressablesAssetLoader.UnloadAsset` no longer calls `GC.Collect()`, `GC.WaitForPendingFinalizers()`, or `Resources.UnloadUnusedAssets()`. The method now only decrements the Addressables reference count. The old implementation caused PlayMode Test Runner crashes on macOS and O(total-assets-in-memory) main-thread stalls per per-asset release. Callers that need memory reclamation should invoke `Resources.UnloadUnusedAssets()` themselves at appropriate moments (scene transitions, boot, memory-pressure events); Unity also runs an unused-assets sweep automatically on `LoadSceneMode.Single` scene loads.
- Corrected `IAssetLoader.UnloadAsset` XML documentation: removed the incorrect "will also destroy GameObject instances" claim — `Addressables.Release(gameObject)` does not destroy the instance; callers must `Object.Destroy` it separately.
- `IAsyncCoroutine.StopCoroutine(bool triggerOnComplete)` now honors its `triggerOnComplete` parameter and flips `IsCompleted` to `true` and `IsRunning` to `false` after stopping. The previous implementation always invoked `OnComplete` callbacks regardless of the flag and left state flags unchanged, so consumers could not distinguish a stopped coroutine from a running one and `triggerOnComplete: false` was silently ignored.
- `GameObjectPool.Dispose()` and `GameObjectPool<T>.Dispose()` now skip pooled entries whose underlying `GameObject` has already been destroyed by an external owner (e.g. a parent GameObject was destroyed while pooled instances were still reparented under it via `DespawnToSampleParent`).

**Breaking Changes** — see `MIGRATION.md` for details:
- Pool types moved from `GameLovers.Services` to `GameLovers.Services.Pooling` (`IPoolService`, `IObjectPool`, `IObjectPool<T>`, `ObjectPool<T>`, `ObjectPoolBase<T>`, `GameObjectPool`, `GameObjectPool<T>`, `IPoolEntitySpawn`, `IPoolEntitySpawn<T>`, `IPoolEntityDespawn`, `IPoolEntityObject<T>`). `PoolService` concrete class remains in `GameLovers.Services`.
- Command contract types moved from `GameLovers.Services` to `GameLovers.Services.Commands` (`IGameCommandBase`, `IGameCommand<>`, `IGameServerCommand<>`, `ICommandService<>`). `CommandService<>` concrete class remains in `GameLovers.Services`.
- `GameLovers.AssetsImporter.*` renamed to `GameLovers.Services.AssetsImporter.*`
- `GameLovers.AssetsImporter.AssetResolverService` is now `GameLovers.Services.AssetResolverService`
- `GameLovers.Services.Editor.*` (versioning editor) renamed to `GameLovers.Services.Versioning.Editor.*`
- `IAssetLoader.UnloadAssetAsync<T>(T, Action)` → `IAssetLoader.UnloadAsset<T>(T, Action)`: method renamed (dropped `Async` suffix) and return type changed from `UniTask` to `void` to reflect its synchronous nature. Replace `await loader.UnloadAssetAsync(x);` with `loader.UnloadAsset(x);`.
- Code generated by `AddressableIdsGenerator` must be re-generated (updated emitted `using` statement)

## [1.0.1] - 2026-01-14

**Changed**:
- Updated dependency `com.gamelovers.dataextensions` to `com.gamelovers.gamedata`
- Updated assembly definitions to reference `GameLovers.GameData`

## [1.0.0] - 2026-01-11

**New**:
- Added *AGENTS.md* document to help guide AI coding assistants to understand and work with this package library
- Added an entire test suit of unit/integration/performance/smoke tests to cover all the code for all services in this package

**Changed**:
- Changed *VersionServices* namespace from *GameLovers* to *GameLovers.Services* to maintain consistency with other services in the package.
- Made *CallOnSpawned*, *CallOnSpawned\<TData\>*, and *CallOnDespawned* methods virtual in *ObjectPoolBase\<T\>* to allow derived pool classes to customize lifecycle callback behavior.

**Fixed**:
- Fixed the *README.md* file to now follow best practices in OSS standards for Unity's package library projects
- Fixed linter warnings in *VersionServices.cs* (redundant field initialization, unused lambda parameter, member shadowing)
- Fixed *GameObjectPool* not invoking *IPoolEntitySpawn.OnSpawn()* and *IPoolEntityDespawn.OnDespawn()* on components attached to spawned GameObjects.

## [0.15.1] - 2025-09-24

**New**:
- Added protected property for all private fields in *CommandService* for access to any game specific project inheritance

## [0.15.0] - 2025-01-05

**New**:
- Added *StartDelayCall* method to *ICoroutineService* to allow deferred methods to be safely executed within the bounds of a Unity Coroutine
- Added the possibility to know the current state of an *IAsyncCoroutine*
- Added the access to the Sample Entity used to generete new entites within an *IObjectPool<T>* and destroy it when disposing the object pool
- Added the possibility to reset an *IObjectPool<T>* to a new state

## [0.14.1] - 2024-11-30

**Fixed**:
- Fixed the *RngLogic.Range(float, float, bool)* method to allow having the same min and max values with maxInclusive set to true

## [0.14.0] - 2024-11-15

**New**:
- Added *PublishSafe* method to *IMessageBrokerService* to allow publishing messages safely in chase of chain subscriptions during publishing of a message

**Changed**:
- *Subscribe* and *Unsubscribe* throw an *InvalidOperationException* when being executed during a message being published

**Fixed**:
- CoroutineTests issues running when building released projects

## [0.13.1] - 2024-11-04

**Fixed**:
- Fixed the *IInstaller* when trying to bind multiple interfaces at the same time

## [0.13.0] - 2024-11-04

**Changed**:
- Changed *CommandService* to now receive the *MessageBrokerService* in the command and help communication with the game architecture

## [0.12.2] - 2024-11-02

**Fixed**:
- Fixed an inssue where *IPoolEntityObject<T>.Init()* wouldn't be called when spawning entities

## [0.12.1] - 2024-10-25

**Fixed**:
- The endless loop when calling *RngService.Range()*
- The endless loop *GameObjectPool* when spawning new entities

## [0.12.0] - 2024-10-22

**New**:
- Added *IRngData* to *PoolService* to suppprt read only data structure and allow abtract injection of data into other objects

**Changed**:
- Changed *RngData* to a class in orther to avoid boxing/unboxing performance when injecting *IRngData*.

## [0.11.0] - 2024-10-19

**New**:
- Added *Spawn<T>(T data)* method to *PoolService* to allow spawning new objects with defined spawning data
- Added *GetPool<T>()* && *TryGetPool<T>()* methods to *PoolService* to allow requesting the pool object maintained by the pool service.

**Changed**:
- Removed *IsSpawned<T>()* method from *PoolService* because is not a fundamental function and can now be accessed from the Pool requested from *GetPool()*
- Now *Spawn<T>(T data)* also invokes *OnSpawn()* without data so objects that implement *IPoolEntitySpawn* have the entire behaviour lifecycle

## [0.10.0] - 2024-10-11

**New**:
- Updated *CommandService* to allow non struct type commands to be executed for reference type commands
- Added *Spawn<T>(T data)* method to pool object to allow spawning new objects with defined spawning data

## [0.9.0] - 2024-08-10

**New**:
- Updated interfaces and classes related to data services, enhancing modularity and improving version handling.
- Added classes for Git commands, version management, and random number generation.

**Changed**:
- Restructured the data service interfaces, consolidating functionality into a single *IDataService* interface and removing unnecessary interfaces.
- Changed *AddData* to *AddOrReplaceData* in the *DataService* implementation.
- Removed the *isLocal* state from data handling.

## [0.8.1] - 2023-08-27

**New**:
- Added GitEditorProcess class to run Git commands as processes, enabling checks for valid Git repositories, retrieving current branch names, commit hashes, and diffs from given commits.
- Introduced *VersionEditorUtils* class for managing application versioning. This includes setting and saving the internal version before building, loading version data from disk, and generating an internal version suffix based on Git information and build settings.

**Changed**:
- Enhanced *IInstaller* interface with new methods for binding multiple type interfaces to a single instance, improving modularity and code organization.

## [0.8.0] - 2023-08-05

**New**:
- Introduced *MainInstaller*, a singleton class for managing instances in the project.
- Added *RngService* for generating and managing random numbers.
- Implemented VersionServices to manage application version, including asynchronous loading of version data and comparison of version strings.

## [0.7.1] - 2023-07-28

**Changed**:
- Tests have been moved to proper folders, and the package number has been updated.
- An unused namespace import has been removed from the InstallerTest class.

**Fixed**:
- Compilation errors in various test files and the PoolService class have been fixed.

## [0.7.0] - 2023-07-28

**New**:
- Introduced a code review process using GitHub Actions workflow.
- Added *IInstaller* interface and Installer implementation for binding and resolving instances.
- Updated namespaces, removed unused code, and modified method calls in test classes.

**Changed**:
- Removed dependency on *ICommandNetworkService *and SendCommand method in *CommandService*.
- Updated *IDataService* interface and *DataService* class to handle local and online data saving.
- Improved readability of *MessageBrokerService* class by using var for type inference.
- Removed unused network service related interfaces, classes, and methods.
- Modified calculation of overFlow in TickService to check for zero DeltaTime.

## [0.6.2] - 2020-09-10

**Changed**:
- Made *NetworkService* abstract and removed *INetworkService* to make easier to work with
- Improved Readme documentation

## [0.6.1] - 2020-09-09

**New**:
- Added connection between *NetworkService* & *CommandService*
- Added integration tests

## [0.6.0] - 2020-09-09

**New**:
- Added *NetworkService*
- Improved Readme documentation

## [0.5.0] - 2020-07-10

**Changed**:
- Renamed *IDataWriter* and it's *FlushData* methods to *IDataSaver* & *SaveData* respectively to match with it's execution logic scope
- Moved the *AddData* to the *IDataService* to allow the *IDataSaver* have the single responsibility of saving data into disk

## [0.4.1] - 2020-07-09

**New**:
- Added *CommandService*

## [0.4.0] - 2020-07-09

**New**:
- Added *DataService*

## [0.3.1] - 2020-02-25

**Fixed**:
- Fixed object pool despawn all elements. It was not despawning all the elements
- Fixed issue preventing to stop coroutines and thrown MissingReferenceException

## [0.3.0] - 2020-02-09

**Changed**:
- Now the *MainInstaller* checks the object binding relationship in compile time
- Improved the *ObjectPools* helper classes with a now static global instatiator for game objects.
- Now the *PoolService* is only a service container for objects pools and no longer creates/initializes new pools.
- Removed *Pool.Clear* functionality. Use *DespawnAll* or delete the pool instead

**Fixed**:
- The *CoroutineService* no longer fails on null coroutines

## [0.2.0] - 2020-01-19

- Added new *ObjectPool* & *GameObjectPool* pools to allow to allow to use object pools independent from the *PoolService*. This allows to have different pools of the same type in the project in different object controllers
- Added new interface *IPoolEntityClear* that allows a callback method for entities when they are cleared from the pool
- Added new unit tests for the *ObjectPool*

**Changed**:
- Now the *PoolService.Clear()* does not take any action parameters. To have a callback when the entity is cleared, please have the entity implement the *IPoolEntityClear* interface

## [0.1.1] - 2020-01-06

**New**:
- Added License

## [0.1.0] - 2020-01-06

- Initial submission for package distribution
