using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace GameLovers.Services.AddressableIds.Editor
{
	/// <summary>
	/// Editor-only project-level settings for the Addressable Ids Generator.
	/// Persisted to <c>ProjectSettings/AddressableIdsEditorSettings.asset</c> via <see cref="ScriptableSingleton{T}"/>.
	/// </summary>
	[FilePath("ProjectSettings/AddressableIdsEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	internal sealed class AddressableIdsEditorSettings : ScriptableSingleton<AddressableIdsEditorSettings>
	{
		[SerializeField] private string _scriptFilename = "AddressableId";
		[SerializeField] private string _namespace = "Game.Ids";
		[SerializeField] private string _addressableLabel = "GenerateIds";

		// ---- Last-generation snapshot (persisted) ----
		[SerializeField] private long _lastGenerationUtcTicks;
		[SerializeField] private int _lastGenerationIdCount;
		[SerializeField] private int _lastGenerationLabelCount;
		[SerializeField] private string _lastGenerationFilenameUsed;
		[SerializeField] private string _lastGenerationLabelFilterUsed;
		[SerializeField] private string[] _lastGenerationAddresses = Array.Empty<string>();
		[SerializeField] private string[] _lastGenerationLabels = Array.Empty<string>();

		/// <summary>Name of the generated C# file (without extension) and the enum/class it contains.</summary>
		public string ScriptFilename
		{
			get => string.IsNullOrWhiteSpace(_scriptFilename) ? "AddressableId" : _scriptFilename;
			set
			{
				var trimmed = (value ?? "AddressableId").Trim();
				if (_scriptFilename == trimmed)
				{
					return;
				}

				_scriptFilename = trimmed;
				Save(true);
			}
		}

		/// <summary>C# namespace for the generated file.</summary>
		public string Namespace
		{
			get => string.IsNullOrWhiteSpace(_namespace) ? "Game.Ids" : _namespace;
			set
			{
				var trimmed = (value ?? "Game.Ids").Trim();
				if (_namespace == trimmed)
				{
					return;
				}

				_namespace = trimmed;
				Save(true);
			}
		}

		/// <summary>Addressables label used to filter which assets get Ids generated. Empty = generate all.</summary>
		public string AddressableLabel
		{
			get => _addressableLabel ?? "";
			set
			{
				var trimmed = (value ?? "").Trim();
				if (_addressableLabel == trimmed)
				{
					return;
				}

				_addressableLabel = trimmed;
				Save(true);
			}
		}

		// ---- Last-generation snapshot accessors ----

		/// <summary>True when a generation snapshot has been recorded by <see cref="RecordGeneration"/>.</summary>
		public bool HasSnapshot => _lastGenerationUtcTicks != 0L;

		/// <summary>UTC timestamp of the last successful generation, or <c>default(DateTime)</c> when none.</summary>
		public DateTime LastGenerationUtc => _lastGenerationUtcTicks == 0L
			? default
			: new DateTime(_lastGenerationUtcTicks, DateTimeKind.Utc);

		/// <summary>How many ids the last generation emitted.</summary>
		public int LastGenerationIdCount => _lastGenerationIdCount;
		/// <summary>How many labels the last generation emitted.</summary>
		public int LastGenerationLabelCount => _lastGenerationLabelCount;
		/// <summary>
		/// Script filename the last generation used; a change from the current setting makes the snapshot stale.
		/// </summary>
		public string LastGenerationFilenameUsed => _lastGenerationFilenameUsed ?? string.Empty;
		/// <summary>
		/// Label filter the last generation used; a change from the current setting makes the snapshot stale.
		/// </summary>
		public string LastGenerationLabelFilterUsed => _lastGenerationLabelFilterUsed ?? string.Empty;

		/// <summary>
		/// Sorted list of addressable addresses that were emitted in the last generation. Empty array when no snapshot.
		/// </summary>
		public IReadOnlyList<string> LastGenerationAddresses => _lastGenerationAddresses ?? Array.Empty<string>();

		/// <summary>
		/// Sorted list of addressable labels that were emitted in the last generation. Empty array when no snapshot.
		/// </summary>
		public IReadOnlyList<string> LastGenerationLabels => _lastGenerationLabels ?? Array.Empty<string>();

		/// <summary>
		/// Records the snapshot of the last successful generation: addresses, labels, and the
		/// generator settings (filename, label filter) that were used at that moment. Both lists are
		/// stored sorted so subsequent set-diffs can be done in O(n+m) without re-sorting at read time.
		/// Persists immediately via <c>Save(true)</c>.
		/// </summary>
		internal void RecordGeneration(IReadOnlyList<string> addresses, IReadOnlyList<string> labels)
		{
			_lastGenerationUtcTicks = DateTime.UtcNow.Ticks;
			_lastGenerationIdCount = addresses?.Count ?? 0;
			_lastGenerationLabelCount = labels?.Count ?? 0;
			_lastGenerationFilenameUsed = ScriptFilename;
			_lastGenerationLabelFilterUsed = AddressableLabel;

			_lastGenerationAddresses = SortedCopy(addresses);
			_lastGenerationLabels = SortedCopy(labels);

			Save(true);
		}

		private static string[] SortedCopy(IReadOnlyList<string> source)
		{
			if (source == null || source.Count == 0)
			{
				return Array.Empty<string>();
			}

			var copy = new string[source.Count];

			for (var i = 0; i < source.Count; i++)
			{
				copy[i] = source[i];
			}

			Array.Sort(copy, StringComparer.Ordinal);
			return copy;
		}

		/// <summary>
		/// Validates <paramref name="identifier"/> for use as a C# script filename / enum name.
		/// Returns <c>true</c> when valid; populates <paramref name="error"/> on failure.
		/// </summary>
		public static bool IsValidIdentifier(string identifier, out string error)
		{
			error = null;

			if (string.IsNullOrWhiteSpace(identifier))
			{
				error = "Identifier cannot be empty.";
				return false;
			}

			var trimmed = identifier.Trim();

			if (char.IsDigit(trimmed[0]))
			{
				error = "Identifier must not start with a digit.";
				return false;
			}

			foreach (var c in trimmed)
			{
				if (!char.IsLetterOrDigit(c) && c != '_')
				{
					error = $"Identifier contains invalid character '{c}'. Only letters, digits, and underscores are allowed.";
					return false;
				}
			}

			return true;
		}

		/// <summary>
		/// Validates <paramref name="ns"/> as a C# namespace string (dot-separated identifiers).
		/// Returns <c>true</c> when valid; populates <paramref name="error"/> on failure.
		/// </summary>
		public static bool IsValidNamespace(string ns, out string error)
		{
			error = null;

			if (string.IsNullOrWhiteSpace(ns))
			{
				error = "Namespace cannot be empty.";
				return false;
			}

			var segments = ns.Trim().Split('.');

			foreach (var segment in segments)
			{
				if (string.IsNullOrEmpty(segment))
				{
					error = "Namespace must not contain consecutive dots or trailing dots.";
					return false;
				}

				if (!IsValidIdentifier(segment, out var segmentError))
				{
					error = $"Namespace segment \"{segment}\" is invalid: {segmentError}";
					return false;
				}
			}

			return true;
		}
	}
}
