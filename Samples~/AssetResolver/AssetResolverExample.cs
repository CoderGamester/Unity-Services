using System;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services.Samples.AssetResolver
{
	/// <summary>
	/// SAMPLE-ONLY driver for the AssetResolver sample. The Canvas hierarchy lives in
	/// <c>AssetResolverUI.prefab</c>; this script holds <c>[SerializeField]</c> references
	/// to the prefab's Buttons / Image / status Text and wires <c>onClick.AddListener</c>
	/// in <see cref="Awake"/>.
	/// </summary>
	/// <remarks>
	/// This sample REQUIRES Addressables setup — the per-sample <c>README.md</c> walks you through
	/// the four steps. If you press Play before completing setup, the driver catches the
	/// <see cref="MissingMemberException"/> from <see cref="AssetResolverService"/> and surfaces a
	/// friendly message via <see cref="_statusText"/> with the next action to take.
	/// </remarks>
	public class AssetResolverExample : MonoBehaviour
	{
		[Tooltip("Drag your SpriteConfigs.asset here. Required.")]
		[SerializeField] private SpriteConfigs _spriteConfigs;

		[Header("UI references (wired by the prefab)")]
		[SerializeField] private Button _loadHeroButton;
		[SerializeField] private Button _loadCoinButton;
		[SerializeField] private Button _loadEnemyButton;
		[SerializeField] private Button _unloadAllButton;
		[SerializeField] private Button _openExplorerButton;
		[SerializeField] private Image    _spriteImage;
		[SerializeField] private TMP_Text _statusText;

		private AssetResolverService _resolver;

		private void Awake()
		{
			_loadHeroButton?.onClick.AddListener(Btn_LoadHero);
			_loadCoinButton?.onClick.AddListener(Btn_LoadCoin);
			_loadEnemyButton?.onClick.AddListener(Btn_LoadEnemy);
			_unloadAllButton?.onClick.AddListener(Btn_UnloadAll);
			_openExplorerButton?.onClick.AddListener(Btn_OpenExplorer);

			// The sample-scoped 'Open in Explorer' menu only exists in the editor; hide
			// the button in player builds so it doesn't sit there as a dead control.
			if (_openExplorerButton != null)
			{
				_openExplorerButton.gameObject.SetActive(Application.isEditor);
			}

			EnsureInputModuleOnEventSystem();
		}

		/// <summary>
		/// Ensures the scene's <see cref="EventSystem"/> has an input module compatible with the
		/// project's Active Input Handling setting. Editor-time scene generation defaults to
		/// <see cref="StandaloneInputModule"/> (legacy); this swaps to
		/// <c>InputSystemUIInputModule</c> when the New Input System is the active package
		/// (<c>ENABLE_INPUT_SYSTEM</c> is defined). Without this swap, the legacy module would
		/// throw <c>InvalidOperationException</c> on <c>UnityEngine.Input.mousePosition</c>
		/// every frame under New-Input-only.
		/// </summary>
		private static void EnsureInputModuleOnEventSystem()
		{
			var es = FindAnyObjectByType<EventSystem>();
			if (es == null)
			{
				return;
			}
			var go = es.gameObject;
#if ENABLE_INPUT_SYSTEM
			if (go.GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>() != null)
			{
				return;
			}
			var legacy = go.GetComponent<StandaloneInputModule>();
			if (legacy != null)
			{
				DestroyImmediate(legacy);
			}
			go.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();
#else
			if (go.GetComponent<StandaloneInputModule>() == null)
			{
				go.AddComponent<StandaloneInputModule>();
			}
#endif
		}

		private void Start()
		{
			_resolver = new AssetResolverService();

			// Bind through MainInstaller so the Services Explorer "Asset Resolver" tab
			// can resolve and inspect the live AssetMap. Mirrors the ServicesPlayground
			// sample's binding pattern.
			MainInstaller.Bind<IAssetResolverService>(_resolver);

			if (_spriteConfigs == null)
			{
				SetStatus("No SpriteConfigs assigned on AssetResolverExample. See the per-sample README.");
				return;
			}

			if (_spriteConfigs.Configs == null || _spriteConfigs.Configs.Count == 0)
			{
				SetStatus("SpriteConfigs has no entries. Drag SpriteId/Sprite pairs into its inspector and re-press Play.");
				return;
			}

			// AddConfigs is a C# 8 default interface method on IAssetAdderService — must be
			// dispatched through the interface (see services package AGENTS.md §4).
			((IAssetAdderService)_resolver).AddConfigs(_spriteConfigs);
			SetStatus($"Registered {_spriteConfigs.Configs.Count} entries. Click a button to load.");
		}

		private void OnDestroy()
		{
			if (_resolver == null || _spriteConfigs == null)
			{
				MainInstaller.Clean();
				return;
			}

			// Avoid the package's "no asset list for the given type" warning when nothing
			// was ever registered (e.g., Play was pressed before completing the Addressables
			// setup from the per-sample README).
			if (_spriteConfigs.Configs != null && _spriteConfigs.Configs.Count > 0)
			{
				_resolver.UnloadAssets<SpriteId, Sprite>(clearReferences: true, _spriteConfigs);
			}

			MainInstaller.Clean();
		}

		// ---------------- Button handlers ----------------

		public void Btn_LoadHero()  => Load(SpriteId.Hero).Forget();
		public void Btn_LoadCoin()  => Load(SpriteId.Coin).Forget();
		public void Btn_LoadEnemy() => Load(SpriteId.Enemy).Forget();

		/// <summary>
		/// Opens the Services Explorer focused on the Asset Resolver tab. Routed via the
		/// sample editor assembly's menu item so this runtime script never has to take a
		/// reference on the package's editor assembly (see services package AGENTS.md §4).
		/// </summary>
		public void Btn_OpenExplorer()
		{
#if UNITY_EDITOR
			UnityEditor.EditorApplication.ExecuteMenuItem(
				"Tools/GameLovers/Samples/Asset Resolver/Open in Explorer");
#endif
		}

		public void Btn_UnloadAll()
		{
			if (_resolver == null || _spriteConfigs == null)
			{
				return;
			}

			if (_spriteConfigs.Configs == null || _spriteConfigs.Configs.Count == 0)
			{
				SetStatus("Nothing to unload — SpriteConfigs has no entries.");
				return;
			}

			_resolver.UnloadAssets<SpriteId, Sprite>(clearReferences: false, _spriteConfigs);
			if (_spriteImage != null)
			{
				_spriteImage.sprite = null;
			}
			SetStatus("Unloaded all sprites (references kept; LoadAsset would re-fetch from Addressables).");
		}

		// ---------------- Loading ----------------

		private async UniTask Load(SpriteId id)
		{
			SetStatus($"Loading {id}...");

			try
			{
				var sprite = await _resolver.RequestAsset<SpriteId, Sprite>(id, instantiate: false);
				if (sprite == null)
				{
					SetStatus($"{id}: AssetReference resolved but Sprite was null (check Addressable setup).");
					return;
				}

				if (_spriteImage != null)
				{
					_spriteImage.sprite = sprite;
				}
				SetStatus($"Loaded {id}: '{sprite.name}'");
			}
			catch (MissingMemberException e)
			{
				SetStatus($"{id}: {e.Message}");
			}
			catch (Exception e)
			{
				SetStatus($"{id}: {e.GetType().Name} — {e.Message}");
				Debug.LogException(e);
			}
		}

		// ---------------- Logging ----------------

		private void SetStatus(string text)
		{
			Debug.Log("[AssetResolverExample] " + text);
			if (_statusText != null)
			{
				_statusText.text = text;
			}
		}
	}
}
