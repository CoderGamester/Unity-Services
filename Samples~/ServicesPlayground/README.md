# Services Playground (Sample)

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. Services is pipeline-neutral. See the [package compatibility matrix](../../README.md#unity-compatibility).

A single-scene, zero-Addressables walk-through that wires every foundation service via `MainInstaller`. Press Play and 10 of the 13 tabs in the **Services Explorer** light up with realistic state.

> **Sample-only types**: `Bullet`, `PlayerData`, `TestMessage`, `PlayerLevelledUpMessage`, `GameLogic`, `LevelUpCommand`, `ServicesBootstrap`, `ServicesPlaygroundUI`. These are NOT part of the `com.gamelovers.services` public API — they live in `GameLovers.Services.Samples.ServicesPlayground` to make that explicit.

---

## How to use

1. Open `ServicesPlayground.unity` (the only scene in this sample folder).
2. Press **Play**.

The scene contains a single `Bootstrap` GameObject with two MonoBehaviours: `ServicesBootstrap` (binds every service via `MainInstaller`) and `ServicesPlaygroundUI` (builds the entire uGUI Canvas + buttons + log + live-status panel at runtime). The bullet pool's "sample entity" is generated programmatically as a sphere primitive — there is no prefab asset to wire up.

---

## What you get

| Service | Buttons | Drives Explorer tab |
|---|---|---|
| `MainInstaller` / `Installer` | Dump bindings · Clean all | **Installer** |
| `IMessageBrokerService` | Subscribe · Publish · PublishSafe · Unsubscribe | **Message Broker** |
| `ITickService` | Sub Update / FixedUpdate / LateUpdate · Unsubscribe All | **Tick** |
| `ICoroutineService` | Delay 2s · Async 3s · Stop All | **Coroutine** |
| `IPoolService` + `GameObjectPool<Bullet>` | Spawn burst · Despawn all | **Pool** |
| `IDataService` | Load PlayerData · Modify + Save · Save All · Delete prefs | **Data** |
| `ITimeService` / `ITimeManipulator` | Add time · Reset | **Time** |
| `IRngService` | Draw · Peek · Restore to 0 | **RNG** |
| `ICommandService<GameLogic>` | Level up | (Message Broker shows the published `PlayerLevelledUpMessage`) |
| `VersionServices` | Dump version | **Versioning** |

The asset-pipeline tabs (`Asset Resolver`, `Assets Importer`, `Addressable Ids`) are demonstrated by the separate **Asset Resolver** sample (it requires a small Addressables Groups setup).

---

## Walking the Services Explorer (E2E protocol)

While the scene is playing, open `Tools > GameLovers > Services Explorer` and walk the tabs in order:

1. **Overview** — every foundation card shows "bound". The three asset-pipeline cards belong to the other sample.
2. **Installer** — eight bindings listed. Per-binding `Clean` / `CleanDispose` removes them; the playground's "Dump bindings" button just logs.
3. **Versioning** — `VersionExternal` is always populated. `VersionInternal`, `Branch`, `Commit`, `BuildNumber` populate after `VersionEditorUtils` writes `version-data.txt` (it does so on every domain reload — see the package `AGENTS.md` §4 *Version Data Pipeline*). Click **Reveal version-data.txt** to confirm.
4. **Message Broker** — click the playground's "Subscribe" then "Publish" buttons. The Explorer's Subscriptions list refreshes every 250 ms. Click **Unsubscribe All** in the Explorer; the playground subscription is gone.
5. **Tick** — click each "Sub …" button; the live-status counters tick up. **Unsubscribe All** in the Explorer; counters freeze.
6. **Coroutine** — click "Delay 2s" or "Async 60 frames"; the `IAsyncCoroutine` shows in the active list with start time. **Stop All Coroutines** aborts.
7. **Pool** — click "Spawn burst"; the pool grows. `DespawnAll` from either side both work.
8. **Data** — click "Load PlayerData", then "Modify + Save". The Data tab shows the JSON snapshot. **Save All Data** is a no-op when there's nothing dirty.
9. **Time** — `AddTime` slider in the Explorer mirrors the playground's "Add time" button (60 s default). `SetInitialTime` re-anchors.
10. **RNG** — the playground's "Draw" advances the counter; "Peek" shows next without advancing. `Restore(count)` from either side rolls the sequence to that point.

---

## Known caveats

- **Versioning fields look empty before first domain reload** — the host project's `Editor/Versioning/VersionEditorUtils` writes `version-data.txt` only on domain reload. If you've imported the sample but haven't reloaded scripts since, save any `.cs` file (or toggle Play) to trigger one, then re-press Play.
- **`Installer.Clean All`** wipes bindings while the playground is running — every other action will then `KeyNotFoundException`. Quit Play and Play again to reset.
- **Input System adaptive** — the driver swaps `EventSystem`'s input module to the right one for your project's Active Input Handling (legacy `StandaloneInputModule` or `InputSystemUIInputModule`) at runtime. Works under "Old", "New", or "Both" without any project setup.
- **TextMeshPro** — UI text is `TextMeshProUGUI`. If your project hasn't imported TMP Essentials yet, Unity prompts once on first Play (one click).

---

## Next sample

For typed asset loading by enum id and the Addressables editor pipeline, see the **Asset Resolver** sample.
