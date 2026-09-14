using System;
using System.Collections.Generic;
using System.IO;
using GameLovers.Services.AddressableIds.Editor;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameLovers.Services.Editor.Explorer.Tabs
{
	/// <summary>
	/// Services Explorer tab for the Addressable Ids Generator.
	/// Replaces the old <c>AddressablesIdGeneratorSettings.asset</c> custom inspector.
	/// Reads/writes settings via <see cref="AddressableIdsEditorSettings"/> and invokes
	/// generation via <see cref="AddressableIdsGeneratorUtils"/>.
	/// </summary>
	public class AddressableIdsTab : ServiceTab
	{
		private const string PendingPlaceholderText = "Click to compute";
		private const string PendingFoldoutText = "Details (click 'Check for stale Ids' first)";
		private static readonly Color _warnColor = new Color(0.95f, 0.75f, 0.30f);
		private static readonly Color _errorColor = new Color(0.9f, 0.5f, 0.4f);
		private static readonly Color _okColor = new Color(0.6f, 0.9f, 0.6f);
		private static readonly Color _mutedColor = new Color(0.7f, 0.7f, 0.7f);

		/// <inheritdoc />
		public override string DisplayName => "Addressable Ids";
		/// <inheritdoc />
		protected override int RefreshIntervalMs => 2000;

		private TextField _filenameField;
		private Label _filenameError;
		private TextField _namespaceField;
		private Label _namespaceError;
		private TextField _labelField;

		private Label _freshnessBanner;
		private Label _outputPathLabel;
		private Label _lastGenerationLabel;

		private Button _checkStaleButton;
		private Label _pendingSummaryLabel;
		private Foldout _pendingFoldout;
		private VisualElement _addedList;
		private VisualElement _removedList;
		private VisualElement _warningsList;

		/// <inheritdoc />
		protected override void BuildUi()
		{
			var scroll = new ScrollView(ScrollViewMode.Vertical);
			scroll.AddToClassList("tab-scroll");

			// ---- Generator Settings section ----
			scroll.Add(MakeSectionLabel("Generator Settings"));

			var settings = AddressableIdsEditorSettings.instance;

			// Script Filename
			_filenameField = new TextField("Script Filename")
			{
				tooltip = "Name of the generated C# file and the enum it contains (no extension).",
				value = settings.ScriptFilename
			};
			_filenameError = MakeInlineError();
			_filenameField.RegisterValueChangedCallback(e =>
			{
				if (AddressableIdsEditorSettings.IsValidIdentifier(e.newValue, out var err))
				{
					settings.ScriptFilename = e.newValue;
					_filenameError.style.display = DisplayStyle.None;
				}
				else
				{
					_filenameError.text = err;
					_filenameError.style.display = DisplayStyle.Flex;
				}

				RefreshOutput();
			});
			scroll.Add(_filenameField);
			scroll.Add(_filenameError);

			// Namespace
			_namespaceField = new TextField("Namespace")
			{
				tooltip = "C# namespace for the generated Addressable Ids file.",
				value = settings.Namespace
			};
			_namespaceError = MakeInlineError();
			_namespaceField.RegisterValueChangedCallback(e =>
			{
				if (AddressableIdsEditorSettings.IsValidNamespace(e.newValue, out var err))
				{
					settings.Namespace = e.newValue;
					_namespaceError.style.display = DisplayStyle.None;
				}
				else
				{
					_namespaceError.text = err;
					_namespaceError.style.display = DisplayStyle.Flex;
				}
			});
			scroll.Add(_namespaceField);
			scroll.Add(_namespaceError);

			// Addressable Label filter
			_labelField = new TextField("Addressable Label")
			{
				tooltip = "Label filter for Addressables asset entries. Leave empty to include all non-read-only groups.",
				value = settings.AddressableLabel
			};
			_labelField.RegisterValueChangedCallback(e =>
			{
				settings.AddressableLabel = e.newValue;
			});
			scroll.Add(_labelField);

			// ---- Output section ----
			scroll.Add(MakeSectionLabel("Output"));

			_freshnessBanner = new Label();
			_freshnessBanner.style.fontSize = 11;
			_freshnessBanner.style.marginBottom = 2;
			_freshnessBanner.style.unityFontStyleAndWeight = FontStyle.Bold;
			scroll.Add(_freshnessBanner);

			_outputPathLabel = new Label();
			_outputPathLabel.AddToClassList("json-preview");
			scroll.Add(_outputPathLabel);

			_lastGenerationLabel = new Label();
			_lastGenerationLabel.style.fontSize = 10;
			_lastGenerationLabel.style.color = new StyleColor(_mutedColor);
			_lastGenerationLabel.style.marginTop = 2;
			scroll.Add(_lastGenerationLabel);

			var outputBar = MakeActionBar();
			outputBar.Add(new Button(OnRevealFile) { text = "Reveal file" });
			outputBar.Add(new Button(OnRevealSettings) { text = "Open Settings file" });
			scroll.Add(outputBar);

			// ---- Pending changes section ----
			scroll.Add(MakeSectionLabel("Pending changes"));

			var pendingRow = new VisualElement();
			pendingRow.AddToClassList("row");
			_checkStaleButton = new Button(OnCheckStale) { text = "Check for stale Ids" };
			_checkStaleButton.tooltip = "Scans Addressables and diffs against the last generation snapshot. " +
			                            "On-demand only — not run on tab refresh.";
			pendingRow.Add(_checkStaleButton);

			_pendingSummaryLabel = new Label(PendingPlaceholderText);
			_pendingSummaryLabel.AddToClassList("row-label");
			_pendingSummaryLabel.style.color = new StyleColor(_mutedColor);
			pendingRow.Add(_pendingSummaryLabel);
			scroll.Add(pendingRow);

			_pendingFoldout = new Foldout { text = PendingFoldoutText, value = false };

			_addedList = new VisualElement();
			_removedList = new VisualElement();
			_warningsList = new VisualElement();
			_pendingFoldout.Add(_addedList);
			_pendingFoldout.Add(_removedList);
			_pendingFoldout.Add(_warningsList);
			scroll.Add(_pendingFoldout);

			Add(scroll);

			// ---- Bottom action bar ----
			var mainBar = MakeActionBar();
			mainBar.Add(MakePrimaryButton("Generate Addressable Ids", OnGenerate));
			mainBar.Add(new Button(OnOpenAddressablesGroups) { text = "Open Addressables Groups" });
			Add(mainBar);

			RefreshOutput();
		}

		/// <inheritdoc />
		protected override void Refresh()
		{
			var settings = AddressableIdsEditorSettings.instance;
			_filenameField.SetValueWithoutNotify(settings.ScriptFilename);
			_namespaceField.SetValueWithoutNotify(settings.Namespace);
			_labelField.SetValueWithoutNotify(settings.AddressableLabel);
			RefreshOutput();
		}

		private void RefreshOutput()
		{
			var settings = AddressableIdsEditorSettings.instance;
			var freshness = AddressableIdsGeneratorUtils.ComputeFreshness(settings);

			_outputPathLabel.text = freshness.ScriptPath;

			if (!freshness.ScriptExists)
			{
				_freshnessBanner.text = "Not generated yet — click 'Generate Addressable Ids'.";
				_freshnessBanner.style.color = new StyleColor(_errorColor);
				_freshnessBanner.style.display = DisplayStyle.Flex;
			}
			else if (freshness.IsStale)
			{
				_freshnessBanner.text = $"Out of date — Addressables modified after the file was generated " +
				                         $"({freshness.LatestSourceWriteTime:HH:mm:ss} > {freshness.ScriptWriteTime:HH:mm:ss}).";
				_freshnessBanner.style.color = new StyleColor(_warnColor);
				_freshnessBanner.style.display = DisplayStyle.Flex;
			}
			else
			{
				_freshnessBanner.text = $"Up to date — script newer than Addressables ({freshness.ScriptWriteTime:HH:mm:ss}).";
				_freshnessBanner.style.color = new StyleColor(_okColor);
				_freshnessBanner.style.display = DisplayStyle.Flex;
			}

			if (settings.HasSnapshot)
			{
				var relative = FormatRelative(settings.LastGenerationUtc);
				var filterTxt = string.IsNullOrEmpty(settings.LastGenerationLabelFilterUsed)
					? "(none)"
					: settings.LastGenerationLabelFilterUsed;
				_lastGenerationLabel.text =
					$"Last generated: {relative} — {settings.LastGenerationIdCount} ids, " +
					$"{settings.LastGenerationLabelCount} labels (filename: {settings.LastGenerationFilenameUsed}, filter: {filterTxt}).";
				_lastGenerationLabel.style.display = DisplayStyle.Flex;
			}
			else
			{
				_lastGenerationLabel.text = "Last generated: — (no snapshot recorded yet).";
				_lastGenerationLabel.style.display = DisplayStyle.Flex;
			}
		}

		private void ResetPendingChanges()
		{
			_pendingSummaryLabel.text = PendingPlaceholderText;
			_pendingSummaryLabel.style.color = new StyleColor(_mutedColor);
			_pendingFoldout.text = PendingFoldoutText;
			_pendingFoldout.value = false;
			_addedList.Clear();
			_removedList.Clear();
			_warningsList.Clear();
		}

		private void OnCheckStale()
		{
			_checkStaleButton.SetEnabled(false);
			try
			{
				var settings = AddressableIdsEditorSettings.instance;
				var diff = AddressableIdsGeneratorUtils.Diff(settings);

				_pendingSummaryLabel.text = FormatSummary(diff);
				_pendingSummaryLabel.style.color = new StyleColor(SummaryColor(diff));

				PopulateDiffFoldout(diff);
				_pendingFoldout.text = $"Details ({TotalDiffCount(diff)} entries) — click to expand";
			}
			finally
			{
				_checkStaleButton.SetEnabled(true);
			}
		}

		private void PopulateDiffFoldout(DiffResult diff)
		{
			_addedList.Clear();
			_removedList.Clear();
			_warningsList.Clear();

			AppendDiffSubsection(_addedList, $"Added addresses ({diff.AddedAddresses.Count})", diff.AddedAddresses, _okColor);
			AppendDiffSubsection(_addedList, $"Added labels ({diff.AddedLabels.Count})", diff.AddedLabels, _okColor);

			AppendDiffSubsection(_removedList, $"Removed addresses ({diff.RemovedAddresses.Count})", diff.RemovedAddresses, _errorColor);
			AppendDiffSubsection(_removedList, $"Removed labels ({diff.RemovedLabels.Count})", diff.RemovedLabels, _errorColor);

			AppendDiffSubsection(_warningsList,
				$"Sanitized-name collisions ({diff.SanitizedNameCollisions.Count})",
				diff.SanitizedNameCollisions, _warnColor);
			AppendDiffSubsection(_warningsList,
				$"Entries with null AssetType ({diff.NullAssetTypeAddresses.Count})",
				diff.NullAssetTypeAddresses, _warnColor);

			if (!diff.HasSnapshot)
			{
				var lbl = new Label("No snapshot recorded yet — generate once to enable add/remove diffs.");
				lbl.style.fontSize = 10;
				lbl.style.color = new StyleColor(_mutedColor);
				lbl.style.marginTop = 4;
				_addedList.Add(lbl);
			}
		}

		private static void AppendDiffSubsection(VisualElement parent, string heading, IReadOnlyList<string> items, Color accent)
		{
			var head = new Label(heading);
			head.style.fontSize = 11;
			head.style.unityFontStyleAndWeight = FontStyle.Bold;
			head.style.marginTop = 4;
			head.style.color = new StyleColor(accent);
			parent.Add(head);

			if (items == null || items.Count == 0)
			{
				var empty = new Label("— none —");
				empty.AddToClassList("tab-empty-label");
				parent.Add(empty);
				return;
			}

			for (var i = 0; i < items.Count; i++)
			{
				var row = new Label(items[i]);
				row.AddToClassList("row-label-mono");
				parent.Add(row);
			}
		}

		private static string FormatSummary(DiffResult diff)
		{
			var prefix = string.Empty;

			if (diff.HasSnapshot && (diff.FilenameChangedSinceSnapshot || diff.LabelFilterChangedSinceSnapshot))
			{
				prefix = "[settings changed since last gen] ";
			}
			else if (!diff.HasSnapshot)
			{
				prefix = "[no snapshot] ";
			}

			return $"{prefix}+{diff.AddedAddresses.Count} added \u00b7 -{diff.RemovedAddresses.Count} removed \u00b7 " +
			       $"{diff.AddedLabels.Count} new label{(diff.AddedLabels.Count == 1 ? string.Empty : "s")} \u00b7 " +
			       $"{diff.SanitizedNameCollisions.Count} collision{(diff.SanitizedNameCollisions.Count == 1 ? string.Empty : "s")} \u00b7 " +
			       $"{diff.NullAssetTypeAddresses.Count} null type{(diff.NullAssetTypeAddresses.Count == 1 ? string.Empty : "s")}";
		}

		private static Color SummaryColor(DiffResult diff)
		{
			return TotalDiffCount(diff) == 0 ? _okColor : _warnColor;
		}

		private static int TotalDiffCount(DiffResult diff)
		{
			return diff.AddedAddresses.Count + diff.RemovedAddresses.Count
			       + diff.AddedLabels.Count + diff.RemovedLabels.Count
			       + diff.SanitizedNameCollisions.Count + diff.NullAssetTypeAddresses.Count;
		}

		private static string FormatRelative(DateTime utc)
		{
			if (utc == default)
			{
				return "—";
			}

			var span = DateTime.UtcNow - utc;

			if (span.TotalSeconds < 60) return $"{(int)span.TotalSeconds}s ago";
			if (span.TotalMinutes < 60) return $"{(int)span.TotalMinutes}m ago";
			if (span.TotalHours < 48) return $"{(int)span.TotalHours}h ago";
			return $"{(int)span.TotalDays}d ago";
		}

		private void OnGenerate()
		{
			var settings = AddressableIdsEditorSettings.instance;

			if (!AddressableIdsEditorSettings.IsValidIdentifier(settings.ScriptFilename, out var idError))
			{
				Debug.LogWarning($"[ServicesExplorer] Cannot generate: {idError}");
				return;
			}

			if (!AddressableIdsEditorSettings.IsValidNamespace(settings.Namespace, out var nsError))
			{
				Debug.LogWarning($"[ServicesExplorer] Cannot generate: {nsError}");
				return;
			}

			AddressableIdsGeneratorUtils.Generate(settings);

			RefreshOutput();
			ResetPendingChanges();
		}

		private void OnRevealFile()
		{
			var settings = AddressableIdsEditorSettings.instance;
			var scriptPath = AddressableIdsGeneratorUtils.ResolveScriptPath(settings);
			var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
			var absPath = Path.Combine(projectRoot, scriptPath);

			if (File.Exists(absPath))
			{
				EditorUtility.RevealInFinder(absPath);
				return;
			}

			Debug.LogWarning($"[ServicesExplorer] {settings.ScriptFilename}.cs not found.");
		}

		private static void OnRevealSettings()
		{
			var projectRoot = Directory.GetParent(Application.dataPath)?.FullName ?? Application.dataPath;
			var absPath = Path.Combine(projectRoot, "ProjectSettings", "AddressableIdsEditorSettings.asset");
			EditorUtility.RevealInFinder(absPath);
		}

		private static void OnOpenAddressablesGroups()
		{
			EditorApplication.ExecuteMenuItem("Window/Asset Management/Addressables/Groups");
		}

		private static Label MakeInlineError()
		{
			var lbl = new Label();
			lbl.style.fontSize = 10;
			lbl.style.color = new StyleColor(new Color(1f, 0.5f, 0.4f));
			lbl.style.marginBottom = 2;
			lbl.style.display = DisplayStyle.None;
			return lbl;
		}
	}
}
