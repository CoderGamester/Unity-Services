using System;
using System.Reflection;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.Services.Editor.Explorer.Tabs
{
	/// <summary>
	/// Displays all data entries held by <see cref="IDataService"/>.
	/// Supports Save, Load, and deleting the saved value from the service's storage.
	/// </summary>
	public class DataTab : ServiceTab
	{
		/// <inheritdoc />
		public override string DisplayName => "Data";

		private ScrollView _scroll;
		private VisualElement _list;
		private Label _countLabel;

		/// <inheritdoc />
		protected override void BuildUi()
		{
			var header = new VisualElement();
			header.AddToClassList("tab-header-row");
			_countLabel = new Label("Entries: 0");
			_countLabel.AddToClassList("tab-section-label");
			header.Add(_countLabel);
			Add(header);

		_scroll = new ScrollView(ScrollViewMode.Vertical);
		_scroll.AddToClassList("tab-scroll");
		_list = new VisualElement();
		_scroll.Add(_list);
		Add(_scroll);

		var bar = MakeActionBar();
		bar.Add(MakePrimaryButton("Save All Data", OnSaveAll));
		Add(bar);
	}

		/// <inheritdoc />
		protected override void Refresh()
		{
			_list.Clear();

			// Hide entries in edit mode (initial OR after a play session ended) regardless
			// of any leftover DataService kept alive by a static field. Together with
			// OnExitingPlayMode() this guarantees the data list does not retain a live
			// snapshot after Stop, even if the consumer's bootstrap forgot to call
			// MainInstaller.Clean() in OnDestroy.
			if (!EditorApplication.isPlaying)
			{
				_countLabel.text = "Entries: 0";
				_list.Add(MakeEmptyLabel());
				return;
			}

			var dataService = TryResolve<IDataService>() as DataService;

			if (dataService == null)
			{
				_countLabel.text = "IDataService not bound";
				_list.Add(MakeEmptyLabel("IDataService not bound"));
				return;
			}

			var entries = dataService.DataEntries;
			_countLabel.text = $"Entries: {entries.Count}";

			if (entries.Count == 0)
			{
				_list.Add(MakeEmptyLabel());
				return;
			}

			foreach (var kvp in entries)
			{
				var dataType = kvp.Key;
				var data = kvp.Value;
				var json = TrySerializeJson(data);
				var storageKey = dataService.GetStorageKey(dataType);
				var isSaved = dataService.Storage.HasKey(storageKey);

				var container = new VisualElement();
				container.style.marginBottom = 6;

				var row = MakeRow(dataType.Name, isSaved ? "[saved]" : "[in-memory only]");

				var saveBtn = MakeRowButton("Save", () => OnSave(dataService, dataType));
				row.Add(saveBtn);

				var loadBtn = MakeRowButton("Load", () => { OnLoad(dataService, dataType); Refresh(); });
				row.Add(loadBtn);

				var deleteBtn = MakeRowButton("Del Saved", () => OnDeleteSaved(dataService, storageKey), danger: true);
				row.Add(deleteBtn);

				container.Add(row);

				if (!string.IsNullOrEmpty(json))
				{
					var jsonLabel = new Label(json);
					jsonLabel.AddToClassList("json-preview");
					container.Add(jsonLabel);
				}

				_list.Add(container);
			}
		}

		// Forcibly clear the data entries widget synchronously when the user stops play
		// mode. Belt-and-braces against bootstraps that fail to call MainInstaller.Clean()
		// in OnDestroy — the DataService entries dictionary lives on the service instance
		// and would otherwise surface as a stale snapshot in edit mode.
		/// <inheritdoc />
		protected override void OnExitingPlayMode()
		{
			_countLabel.text = "Entries: 0";
			_list.Clear();
			_list.Add(MakeEmptyLabel());
		}

		private void OnSaveAll()
	{
		var dataService = TryResolve<IDataService>();
		dataService?.SaveAllData();
		Refresh();
	}

	private static string TrySerializeJson(object data)
		{
			try
			{
				return JsonConvert.SerializeObject(data, Formatting.Indented);
			}
			catch
			{
				return null;
			}
		}

		private void OnSave(DataService dataService, Type dataType)
		{
			try
			{
				var method = typeof(DataService)
					.GetMethod(nameof(IDataService.SaveData))
					?.MakeGenericMethod(dataType);
				method?.Invoke(dataService, null);
				Refresh();
			}
			catch (Exception e)
			{
				Debug.LogError($"[ServicesExplorer] SaveData threw: {e.Message}");
			}
		}

		private void OnLoad(DataService dataService, Type dataType)
		{
			try
			{
				var method = typeof(DataService)
					.GetMethod(nameof(IDataService.LoadData))
					?.MakeGenericMethod(dataType);
				method?.Invoke(dataService, null);
			}
			catch (Exception e)
			{
				Debug.LogError($"[ServicesExplorer] LoadData threw: {e.Message}");
			}
		}

		private void OnDeleteSaved(DataService dataService, string key)
		{
			if (!EditorUtility.DisplayDialog("Delete Saved Data",
				$"Delete the saved value \"{key}\"? This cannot be undone.", "Delete", "Cancel"))
			{
				return;
			}

			dataService.Storage.Delete(key);
			Refresh();
		}
	}
}
