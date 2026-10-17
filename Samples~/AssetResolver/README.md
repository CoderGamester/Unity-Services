# Asset Resolver (Sample)

> **Unity compatibility:** Minimum Unity version `6000.0`; reference streams are `6000.0.x`, `6000.3.x`, and `6000.5.x`. Services is pipeline-neutral. See the [package compatibility matrix](../../README.md#unity-compatibility).

A focused demo of `AssetResolverService` end-to-end: register a typed-config ScriptableObject, request an asset by id-enum, unload references when done.

> **Sample-only types**: `SpriteId`, `SpriteConfigs`, `AssetResolverExample`. These are NOT part of the `com.gamelovers.services` public API — they live in `GameLovers.Services.Samples.AssetResolver` to make that explicit.

This sample exercises the three Services Explorer tabs that the **Services Playground** sample doesn't cover: **Asset Resolver**, **Assets Importer**, and **Addressable Ids**. It also exercises the `AssetConfigsScriptableObject` custom inspector and its "Regenerate Addressable Ids" button.

- **Asset Resolver tab** — the sample's `AssetResolverExample` binds `IAssetResolverService` through `MainInstaller` while Play is running, so the tab's live `AssetMap` tree populates automatically. Click the in-scene **Load Hero / Coin / Enemy** buttons and watch the rows flip to `loaded`.
- **Assets Importer tab** — the sample ships `SpriteConfigsImporter` (an empty subclass of `AssetsConfigsImporter<SpriteId, Sprite, SpriteConfigs>`), which the tab discovers by reflection.
- **Addressable Ids tab** — every sprite this sample marks Addressable also gets the label `services-sample-asset-resolver`, so the tab's label-filtered Generate produces a sample-scoped enum without touching the rest of your project.

A blue **Open Services Explorer** button at the bottom of the sample UI opens the window pre-selected on the Asset Resolver tab (Editor only).

---

## Why this sample requires Addressables

`AssetResolverService` is an Addressables wrapper, so anything this sample loads must be marked Addressable in your project. The sample ships with three placeholder sprites (`Sprites/Hero.png`, `Coin.png`, `Enemy.png`) and an empty `SpriteConfigs.asset`; an editor automation handles the wiring on your behalf the moment those sprites land in your project.

If you press Play before the automation finishes (or before you swap in your own sprites), the driver catches the `MissingMemberException` from `AssetResolverService` and surfaces a friendly status message via the on-screen `Status` text.

---

## Setup

The sample's editor automation (`AssetResolverSampleSetup`, fired by an `AssetPostprocessor`) does everything for you on import:

1. Marks every PNG under this sample's `Sprites/` folder as Addressable in a dedicated group `GameLoversServicesSamples_AssetResolver`. Never touches your default group or any other user-defined group.
2. Renames non-canonical filenames to `Hero` / `Coin` / `Enemy` (substring match first, alphabetical fallback). The shipped placeholders are already canonical, so first-import is a no-op.
3. Populates `SpriteConfigs.asset` rows for `SpriteId.Hero` / `Coin` / `Enemy`, pointing each at the matching sprite via `AssetReference`. **Existing user mappings are respected** — if a row already points at a different sprite, the automation skips it.
4. Logs a single summary line, e.g. `[AssetResolverSample] Setup complete. Group: 'GameLoversServicesSamples_AssetResolver', sprites in group: 3, configs entries set: 3, renamed: 0.`

If Addressables Settings don't exist yet, they are created (default location). The user's first sample import generates `AddressableAssetSettings.asset` and the sample's group together.

### Press Play

Open `AssetResolver.unity` and press Play. Click **Load Hero / Load Coin / Load Enemy** — the resolved sprite renders. **Unload All** releases the Addressables handles (references kept; LoadAsset would re-fetch).

### Swap in your own sprites

Drop your own PNGs into `Assets/Samples/GameLovers Services/<version>/Asset Resolver/Sprites/`. The post-processor fires on import:

- Files named `Hero.png` / `Coin.png` / `Enemy.png` (case-insensitive) are kept as-is.
- Files containing one of those words (e.g. `MyHeroIcon.png`) are renamed to the canonical name.
- Anything else is renamed to fill remaining slots in alphabetical order.

You don't need to modify `SpriteConfigs.asset` by hand — the automation re-runs and updates the rows.

### Re-running setup manually

Two escape hatches if you want to force a re-run (e.g., you deleted the Addressables group, replaced sprites while the editor was closed, or want to verify state):

- **Menu**: `Tools > GameLovers > Samples > Asset Resolver > Refresh Addressables`
- **Inspector button**: select `SpriteConfigs.asset` — the package's custom inspector adds a **Refresh AssetResolver Sample Addressables** button at the bottom (only visible when the inspected asset lives under `Asset Resolver/`).

### Removing the sample

Unity Package Manager does not expose per-sample uninstall — it only ships the package-level Remove. To remove just this sample, delete the imported folder via the **Project window**: right-click `Assets/Samples/GameLovers Services/<version>/Asset Resolver/` → Delete.

The Addressables group `GameLoversServicesSamples_AssetResolver` and the label `services-sample-asset-resolver` are not auto-removed — Unity recompiles the deleted scripts before any deletion-batch callback fires, so the sample's own automation can't observe its own removal. Clean them up manually when you're done with the sample:

- `Window > Asset Management > Addressables > Groups` → right-click the group → **Remove Group**.
- `Labels` (top-right of the Groups window) → delete `services-sample-asset-resolver`.

### Manual fallback (no automation)

If you prefer to set things up by hand (or the automation doesn't apply to your workflow), use the four-step flow:

1. Pick or create three Sprite assets.
2. Mark them Addressable in `Window > Asset Management > Addressables > Groups`.
3. Open `SpriteConfigs.asset` and add three entries: `SpriteId.Hero/Coin/Enemy` → drag your sprites into the `AssetReference` slots.
4. Press Play.

---

## What the sample teaches

| Surface | Demo action |
|---|---|
| `AssetResolverService.AddConfigs<TId, TAsset>` | `Start()` registers `SpriteConfigs` so requests by `SpriteId` resolve. The driver also calls `MainInstaller.Bind<IAssetResolverService>(...)` so editor tooling can introspect the live state. |
| `IAssetResolverService.RequestAsset<TId, TAsset>` | Each "Load" button pulls a sprite by enum id; the result drives a uGUI Image |
| `IAssetResolverService.UnloadAssets<TId, TAsset>` | "Unload All" releases handles via the package's contract; references kept |
| `AssetConfigsScriptableObject<TId, TAsset>` custom inspector | Inspect `SpriteConfigs.asset` — the package ships a custom inspector with diagnostics (duplicate keys, empty GUIDs), a **Regenerate Addressable Ids** button, and (for this sample only) a **Refresh AssetResolver Sample Addressables** button |
| `AssetsConfigsImporter<TId, TAsset, TScriptableObject>` | The sample ships an empty subclass `SpriteConfigsImporter` so the **Assets Importer** tab discovers a real row (Set Path → Import → Select) |
| `AddressableIdsGeneratorUtils` | Use `Tools > GameLovers > Addressable Ids > Open in Explorer` and the label `services-sample-asset-resolver` to regenerate a `SpriteId`-style enum from this sample's Addressables alone (the sample ships a hand-defined `SpriteId` so you can press Play before running the generator) |
| Services Explorer **Asset Resolver** tab | While Play is running, opens the live `AssetMap` tree (asset type → id type → id → ref + loaded status); per-asset Unload + bulk **Unload All** |
| Services Explorer **Assets Importer** tab | Discovered `IAssetConfigsImporter` list with per-importer paths and statuses — `SpriteConfigsImporter` shows up here |
| Services Explorer **Addressable Ids** tab | Generator settings, output status, **Generate Addressable Ids** + **Open Addressables Groups** |

---

## Two ways to populate `SpriteConfigs.asset`

This sample shows both flows on the same data:

1. **`AssetResolverSampleSetup` post-processor (default).** Runs automatically on import. Marks the sample's sprites Addressable, applies the `services-sample-asset-resolver` label, and wires `SpriteConfigs.asset`. **Preserves user mappings** — if a row already points at a sprite you chose by hand, it leaves that row alone.
2. **Assets Importer tab `SpriteConfigsImporter` row (manual).** Open `Tools > GameLovers > Assets Importer / Open in Explorer`, click **Set Path** on the `SpriteConfigsImporter` row, point it at the sample's `Sprites/` folder, click **Import**. The package's general-purpose importer **clears `Configs` and re-fills** from the folder scan — that's the package's documented behavior.

The asymmetry is intentional: flow #1 is a sample-scoped UX optimization; flow #2 demonstrates the package's general importer pipeline, which is what consumers wire up for their own importers.

---

## Try the AddressableIds generator on the sample

Once the post-processor has run (the sample's sprites carry the label `services-sample-asset-resolver`), the Addressable Ids tab can generate a sample-scoped enum:

1. Open `Tools > GameLovers > Addressable Ids / Open in Explorer`.
2. Set the three fields:
   - **Script Filename**: `SampleSpriteIds`
   - **Namespace**: `GameLovers.Services.Samples.AssetResolver.Generated`
   - **Addressable Label**: `services-sample-asset-resolver`
3. Click **Generate Addressable Ids**. A `SampleSpriteIds.cs` file appears under `Assets/` with one entry per sprite the sample marked Addressable.

This does not modify the sample's hand-defined `SpriteId` enum — it just shows the generator producing matching code from the same Addressables. Delete `SampleSpriteIds.cs` (or change the filename / namespace) when you're done.

---

## Troubleshooting

- **"The AssetResolverService does not have the AssetReference config to load …"** — the automation didn't run or `SpriteConfigs.asset` is still empty. Run `Tools > GameLovers > Samples > Asset Resolver > Refresh Addressables` and check the Console for the summary line.
- **"AssetReference resolved but Sprite was null"** — the entry points at an asset that isn't Addressable, or its group was deleted. Re-run the refresh menu (it re-creates the group) or check `Window > Asset Management > Addressables > Groups`.
- **"Menu 'Tools/GameLovers/Samples/Asset Resolver/Refresh Addressables' is unavailable"** — the sample's editor scripts aren't compiled (e.g. you deleted the sample's `Editor/` folder). Re-import the sample from Package Manager.
- **`MissingMethodException: ConfigConverter+Default constructor not found`** — unrelated to this sample. It's a Newtonsoft-JSON quirk; the services package's `DataService` doesn't run in this sample.

---

## Sibling sample

For the foundation services without Addressables, see the **Services Playground** sample.
