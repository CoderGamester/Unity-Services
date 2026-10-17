# GameLovers Services

Small, composable Unity 6 services for installation, messaging, ticking, coroutines, pooling, persistence, RNG, time, commands, versioning, and Addressables workflows.

[![Unity](https://img.shields.io/badge/Unity-6000.0%20%7C%206000.3%20%7C%206000.5-blue.svg)](https://unity.com/download)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE.md)
[![Version](https://img.shields.io/github/v/tag/CoderGamester/Unity-Services?label=version)](CHANGELOG.md)

## When to use it

Use Services when you want independently selectable application services without adopting a full DI container. Use a scoped DI solution when you require hierarchical lifetimes or broad constructor injection. The package is pipeline-neutral.

## Unity compatibility

| Item | Current policy |
| --- | --- |
| Minimum Unity version | `6000.0` |
| Reference streams | `6000.0.x`, `6000.3.x`, `6000.5.x` |
| Reference editors | `6000.0.81f1`, `6000.3.21f1`, `6000.5.7f1` (primary) |
| Render pipeline | Pipeline-neutral |
| Validation status | Compatibility target; fresh matrix artifacts are required for a validated claim. |

## Install

Unity does not resolve Git dependencies transitively. Add the required Git packages explicitly; Addressables and the performance test framework resolve from the Unity registry.

```json
{
  "dependencies": {
    "com.gamelovers.gamedata": "https://github.com/CoderGamester/Unity-GameData.git#1.0.3",
    "com.cysharp.unitask": "https://github.com/Cysharp/UniTask.git?path=src/UniTask/Assets/Plugins/UniTask#2.5.10",
    "com.gamelovers.services": "https://github.com/CoderGamester/Unity-Services.git#2.1.2"
  }
}
```

## First success

Install a service once, resolve it through the same facade, and release only the resources your application owns:

```csharp
using GameLovers.Services;
using UnityEngine;

public sealed class GameBootstrap : MonoBehaviour
{
    private TickService tickService;

    private void Awake()
    {
        tickService = new TickService();
        MainInstaller.Bind<ITickService>(tickService);
    }

    private void OnDestroy()
    {
        MainInstaller.CleanDispose<ITickService>();
    }
}
```

`Clean<T>` unregisters an instance. `CleanDispose<T>` disposes the registered service and unregisters it. Do not dispose a shared static facility unless this bootstrap exclusively owns it.

## Services at a glance

| Area | Key API |
| --- | --- |
| Installation | `MainInstaller` for one global interface; `Installer` for multi-interface binding |
| Messaging | `IMessageBrokerService` pub/sub with explicit unsubscribe ownership |
| Frame work | `ITickService` and `ICoroutineService` |
| Pools | `IPoolService` and object-pool lifecycle hooks |
| State | `IDataService`, `ITimeService`, `IRngService`, commands, and version data |
| Assets | `IAssetLoader`, `ISceneLoader`, and `AssetResolverService` |

Data storage is appropriate for small, non-secret local state. `DataService` persists through `PlayerPrefs` by default; pass `FileDataStorage` for atomic file saves, `BrowserLocalDataStorage` for WebGL saves that survive new builds, and an optional key prefix to scope saves per profile. See [Data Service](docs/data-service.md#storage-backends). Addressables handles must be released by the owner that acquired them; loading and instantiating have different release paths.

For Asset Resolver sample code, call `AddConfigs` through `IAssetAdderService` rather than the concrete resolver, and reference GameData directly from any asmdef that uses its public types.

## Samples, docs, and support

| Sample | Focus |
| --- | --- |
| [Services Playground](Samples~/ServicesPlayground/README.md) | Foundation services in a ready-made scene |
| [Asset Resolver](Samples~/AssetResolver/README.md) | Typed Addressables asset lookup and importer tooling |

Read [docs](docs/README.md), [CHANGELOG.md](CHANGELOG.md), and [MIGRATION.md](MIGRATION.md). Report issues at [Unity-Services](https://github.com/CoderGamester/Unity-Services/issues).
