using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace GameLovers.Services.Versioning.Editor
{
	/// <summary>
	/// Editor-only project-level settings for the versioning pipeline.
	/// Persisted to <c>ProjectSettings/VersioningEditorSettings.asset</c> via <see cref="ScriptableSingleton{T}"/>.
	/// </summary>
	[FilePath("ProjectSettings/VersioningEditorSettings.asset", FilePathAttribute.Location.ProjectFolder)]
	internal sealed class VersioningEditorSettings : ScriptableSingleton<VersioningEditorSettings>
	{
		/// <summary>
		/// Folder under which <c>version-data.txt</c> is written. Must contain a <c>Resources</c> segment.
		/// </summary>
		public const string DefaultFolderPath = "Assets/Configs/Resources";

		[SerializeField] private string _resourcesFolderPath = DefaultFolderPath;

		/// <summary>
		/// Returns the configured project-relative folder path, or <see cref="DefaultFolderPath"/> when unset.
		/// </summary>
		public string ResourcesFolderPath =>
			string.IsNullOrWhiteSpace(_resourcesFolderPath) ? DefaultFolderPath : _resourcesFolderPath;

		/// <summary>
		/// Persists a new resources folder path. Normalises separators to forward-slash before saving.
		/// Call <see cref="IsValidResourcesPath"/> first to ensure the value is acceptable.
		/// </summary>
		public void SetResourcesFolderPath(string relativePath)
		{
			_resourcesFolderPath = relativePath.Trim().Replace('\\', '/');
			Save(true);
		}

		/// <summary>
		/// Validates <paramref name="relativePath"/> for use as the version-data write location.
		/// Returns <c>true</c> when valid; populates <paramref name="error"/> with a human-readable message on failure.
		/// </summary>
		/// <remarks>
		/// Rules enforced:
		/// <list type="bullet">
		///   <item>Must start with <c>Assets/</c></item>
		///   <item>Must not contain <c>..</c> path segments</item>
		///   <item>Must contain a path segment named exactly <c>Resources</c></item>
		///   <item>Must resolve to a path inside the Unity project root</item>
		/// </list>
		/// </remarks>
		public static bool IsValidResourcesPath(string relativePath, out string error)
		{
			error = null;

			if (string.IsNullOrWhiteSpace(relativePath))
			{
				error = "Path cannot be empty.";
				return false;
			}

			var normalised = relativePath.Trim().Replace('\\', '/');

			if (!normalised.StartsWith("Assets/", StringComparison.OrdinalIgnoreCase))
			{
				error = "Path must start with \"Assets/\".";
				return false;
			}

			var segments = normalised.Split('/');
			foreach (var segment in segments)
			{
				if (segment == "..")
				{
					error = "Path must not contain \"..\" segments.";
					return false;
				}
			}

			var containsResources = false;
			foreach (var segment in segments)
			{
				if (string.Equals(segment, "Resources", StringComparison.Ordinal))
				{
					containsResources = true;
					break;
				}
			}

			if (!containsResources)
			{
				error = "Path must contain a folder segment named exactly \"Resources\" so that " +
				        "Resources.Load<TextAsset>(\"version-data\") can find the file at runtime.";
				return false;
			}

			// Ensure the resolved absolute path stays within the project root.
			var projectRoot = Directory.GetParent(UnityEngine.Application.dataPath)?.FullName;
			if (projectRoot == null)
			{
				error = "Could not determine the project root directory.";
				return false;
			}

			var absPath = Path.GetFullPath(Path.Combine(projectRoot, normalised));
			if (!absPath.StartsWith(projectRoot, StringComparison.OrdinalIgnoreCase))
			{
				error = "Path must resolve to a location inside the project directory.";
				return false;
			}

			return true;
		}
	}
}
