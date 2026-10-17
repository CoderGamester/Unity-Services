# Data Service

[← Back to index](README.md)

Cross-platform persistent data storage with JSON serialization and a pluggable storage backend.

**Key Points:**
- Data is keyed by **type** (`typeof(T)`) — no string keys at the API level
- Only **reference types** (`class`) are supported; value types (`struct`) are not
- Storage keys use `typeof(T).Name`, or `{keyPrefix}.{typeof(T).Name}` with a key prefix — watch for name collisions across assemblies with types sharing the same short name
- Data persists through `PlayerPrefs` unless you pass another [storage backend](#storage-backends)
- `GetData<T>()` throws `KeyNotFoundException` if the type has not been loaded or added — use `HasData<T>()` to guard
- `LoadData<T>` calls `Activator.CreateInstance<T>()` when no saved data exists; `T` must have a **parameterless constructor**

```csharp
[Serializable]
public class PlayerData
{
    public string Name;
    public int Level;
    public PlayerData() { }  // required for LoadData<T> when no saved data exists
}

var dataService = new DataService();

// Load from storage (or create fresh if not saved yet)
PlayerData player = dataService.LoadData<PlayerData>();

// Modify in memory
player.Name = "Hero";
player.Level = 10;

// Save one type to storage
dataService.SaveData<PlayerData>();

// Save all loaded types to storage
dataService.SaveAllData();

// Add or replace in memory without saving
dataService.AddOrReplaceData(new PlayerData { Name = "Alt", Level = 5 });

// Read back from memory
PlayerData loaded = dataService.GetData<PlayerData>();

// Guard against missing data
if (dataService.HasData<PlayerData>())
{
    var data = dataService.GetData<PlayerData>();
}
```

## Storage backends

`DataService` serializes each type to JSON and hands the string to an `IDataStorage`. Pass one to the constructor, with an optional key prefix:

```csharp
// Default: PlayerPrefs, keys are typeof(T).Name
var dataService = new DataService();

// One JSON file per type in Application.persistentDataPath
var fileData = new DataService(new FileDataStorage());

// Files in a folder you choose, with saves scoped to one profile
var profileData = new DataService(new FileDataStorage(Path.Combine(Application.persistentDataPath, "Saves")), "Profile1");
```

| Backend | Where data lives | Use for |
|---------|------------------|---------|
| `PlayerPrefsDataStorage` | `PlayerPrefs`, saved after every write | The default; small state on any platform |
| `FileDataStorage` | `{key}.json` files in one directory, `Application.persistentDataPath` by default | Desktop and mobile saves |
| `BrowserLocalDataStorage` | The browser's `localStorage` for the page origin | WebGL players |

- `FileDataStorage` writes each value to `{key}.json.tmp` first and then replaces `{key}.json`, so a crash or power loss mid-save keeps the previous value. Keys must be valid file names.
- On WebGL, `PlayerPrefs` and `Application.persistentDataPath` are stored in IndexedDB under a path that includes the build. Hosts that publish each upload under a new path therefore lose those saves on every update. `localStorage` is keyed by the page origin, so `BrowserLocalDataStorage` saves survive new builds. Several games served from one host origin share one `localStorage`, so give each game its own key prefix.
- `BrowserLocalDataStorage` writes synchronously and throws `IOException` when the browser rejects a write, for example when its quota is full. Its constructor throws `PlatformNotSupportedException` in the Editor and on non-WebGL platforms, and `InvalidOperationException` when the browser blocks `localStorage`, so choose the backend per platform:

```csharp
#if UNITY_WEBGL && !UNITY_EDITOR
IDataStorage storage = new BrowserLocalDataStorage();
#else
IDataStorage storage = new FileDataStorage();
#endif
var dataService = new DataService(storage, "MyGame");
```

Implement `IDataStorage` (`HasKey`, `Read`, `Write`, `Delete`; `Read` returns `null` for a missing key) to persist anywhere else. `Write` must persist the value before it returns.

Changing the backend or the key prefix changes where `LoadData<T>` looks, so data saved under the old backend or prefix is not migrated automatically.

## IDataProvider vs IDataService

| Interface | Methods | Use for |
|-----------|---------|---------|
| `IDataProvider` | `GetData<T>()`, `HasData<T>()` | Read-only consumers |
| `IDataService : IDataProvider` | adds `AddOrReplaceData`, `LoadData`, `SaveData`, `SaveAllData` | Full read-write access |

Bind `IDataService` for systems that need to save; bind `IDataProvider` for read-only consumers to enforce discipline.

## Error Reference

| Call | Exception | Condition |
|------|-----------|-----------|
| `GetData<T>()` | `KeyNotFoundException` | `T` not loaded or added |
| `LoadData<T>()` | `MissingMethodException` | `T` has no parameterless constructor |
| `new DataService(null)` | `ArgumentNullException` | No storage given |
| `new FileDataStorage(...)` methods | `ArgumentException` | Key is not a valid file name |
| `new BrowserLocalDataStorage()` | `PlatformNotSupportedException` | Not running in a WebGL player |
| `new BrowserLocalDataStorage()` | `InvalidOperationException` | The browser blocks `localStorage` |
| `BrowserLocalDataStorage.Write` | `IOException` | The browser rejected the write |
