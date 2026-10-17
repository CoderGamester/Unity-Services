using GameLovers.Services.AssetsImporter;
using GameLovers.GameData;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.AddressableAssets;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services.AssetsImporter.Editor
{
	/// <summary>
	/// Asset importers allow to create custom in editor time processors of specific assets in the project.
	/// They import their correspondent asset type and map it inside a container with their respective id.
	/// </summary>
	public interface IAssetConfigsImporter
	{
		/// <summary>
		/// The type of scriptable object that will be saved. Helper to cast the scriptable object type at runtime
		/// </summary>
		Type ScriptableObjectType { get; }

		/// <summary>
		/// Imports all assets belonging to this asset config scope into a defined container
		/// </summary>
		void Import(string assetsFolderPath = null);
	}

	/// <summary>
	/// Interface for asset importers that generate asset configuration data.
	/// </summary>
	public interface IAssetConfigsGeneratorImporter : IAssetConfigsImporter
	{
		/// <summary>
		/// Gets the name of the identifier type.
		/// </summary>
		string TIdName { get; }

		/// <summary>
		/// Gets the name of the scriptable object type.
		/// </summary>
		string TScriptableObjectName { get; }

		/// <summary>
		/// Gets a value indicating whether the generated script should be cached.
		/// </summary>
		bool CacheScriptAsOld { get; }
	}

	/// <inheritdoc />
	public abstract class AssetsConfigsImporter<TId, TAsset, TScriptableObject> : AssetsConfigsImporterBase<TId, TAsset, TScriptableObject>
		where TId : Enum
		where TScriptableObject : AssetConfigsScriptableObject<TId, TAsset>
	{
		/// <inheritdoc />
		protected override TId[] Ids => Enum.GetValues(typeof(TId)) as TId[];
	}

	/// <inheritdoc cref="IAssetConfigsImporter"/>
	/// <remarks>
	/// It will save the asset data into a scriptable object of <typeparamref name="TScriptableObject"/> type
	/// </remarks>
	public abstract class AssetsConfigsImporterBase<TId, TAsset, TScriptableObject> : IAssetConfigsImporter
		where TScriptableObject : AssetConfigsScriptableObject<TId, TAsset>
	{
		/// <inheritdoc />
		public Type ScriptableObjectType => typeof(TScriptableObject);

		/// <inheritdoc />
		public virtual Type AssetType => typeof(TAsset);

		/// <summary>
		/// Gets an array of identifiers for the assets being imported.
		/// </summary>
		/// <remarks>
		/// This property is abstract and must be implemented by derived classes.
		/// It is used to retrieve the identifiers for the assets being imported.
		/// </remarks>
		protected abstract TId[] Ids { get; }

		/// <inheritdoc />
		public void Import(string assetsFolderPath = null)
		{
			var type = ScriptableObjectType;
			var soAssets = AssetDatabase.FindAssets($"t:{type.Name}");
			var scriptableObject = soAssets.Length > 0
									   ? AssetDatabase.LoadAssetAtPath<TScriptableObject>(AssetDatabase.GUIDToAssetPath(soAssets[0]))
									   : ScriptableObject.CreateInstance<TScriptableObject>();

			if(!string.IsNullOrEmpty(assetsFolderPath))
			{
				scriptableObject.AssetsFolderPath = assetsFolderPath;
			}

			if (soAssets.Length == 0)
			{
				AssetDatabase.CreateAsset(scriptableObject, $"Assets/{type.Name}.asset");
			}

			var assetGuids = new List<string>(AssetDatabase.FindAssets($"t:{AssetType.Name}", new[]
			{
				scriptableObject.AssetsFolderPath
			}));
			var assetsPaths = assetGuids.ConvertAll(a => AssetDatabase.GUIDToAssetPath(a));

			scriptableObject.Configs.Clear();
			scriptableObject.Configs.AddRange(OnImportIds(scriptableObject, assetGuids, assetsPaths));
			OnImportComplete(scriptableObject);
			EditorUtility.SetDirty(scriptableObject);

			Debug.Log($"Finished importing asset data of '{AssetType.Name}' type with '{typeof(TId).Name}' as identifier.\n" +
					  $"To: '{typeof(TScriptableObject).Name}' - From '{scriptableObject.AssetsFolderPath}' ");
		}

		/// <summary>
		/// The filename fragment an id is matched against; override when assets are not named after the id.
		/// </summary>
		protected virtual string IdPattern(TId id)
		{
			return id.ToString();
		}

		/// <summary>
		/// Pairs each id with the asset whose path contains it, skipping ids with no match.
		/// Override to change how ids are mapped onto the discovered assets.
		/// </summary>
		protected virtual List<Pair<TId, AssetReference>> OnImportIds(TScriptableObject scriptableObject,
																	  List<string> assetGuids,
																	  List<string> assetsPaths)
		{
			var ids = Ids;
			var list = new List<Pair<TId, AssetReference>>(ids.Length);

			for (var i = 0; i < ids.Length; i++)
			{
				var indexOf = IndexOfId(IdPattern(ids[i]), assetsPaths);

				if (indexOf < 0)
				{
					continue;
				}

				list.Add(new Pair<TId, AssetReference>(ids[i], new AssetReference(assetGuids[indexOf])));
			}

			return list;
		}

		/// <summary>
		/// Index of the first path containing <c>/{id}.</c>, or -1; the dot is what stops a prefix matching.
		/// </summary>
		protected int IndexOfId(string id, IList<string> assetsPath)
		{
			for (var i = 0; i < assetsPath.Count; i++)
			{
				if (assetsPath[i].Contains($"/{id}."))
				{
					return i;
				}
			}

			return -1;
		}

		/// <summary>Runs after the import has written the asset; the base implementation does nothing.</summary>
		protected virtual void OnImportComplete(TScriptableObject scriptableObject) { }
	}

	/// <inheritdoc />
	public abstract class AssetsConfigsGeneratorImporter<TAsset> : IAssetConfigsGeneratorImporter
	{
		/// <summary>Assembly-qualified name of the id enum the generated script should use.</summary>
		public abstract string TIdName { get; }

		/// <summary>
		/// Assembly-qualified name of the configs ScriptableObject the generated script should target.
		/// </summary>
		public abstract string TScriptableObjectName { get; }

		/// <summary>When true the previous generated script is kept as a backup before being overwritten.</summary>
		public virtual bool CacheScriptAsOld => true;

		/// <inheritdoc />
		public Type ScriptableObjectType => Type.GetType(TScriptableObjectName);

		/// <inheritdoc />
		public void Import(string assetsFolderPath)
		{
			if (assetsFolderPath == null)
			{
				Debug.LogWarning("Assets folder path is null");
				return;
			}

			var type = this.GetType();
			var assetType = typeof(TAsset);
			var thisAsset = AssetDatabase.FindAssets($"{type.Name}");
			var thisAssetPath = AssetDatabase.GUIDToAssetPath(thisAsset[0]);

			if (thisAsset.Length == 0)
			{
				Debug.LogError($"AssetsConfigsGeneratorImporter '{type.Name}' not found to generate");
				return;
			}

			var assetGuids = new List<string>(AssetDatabase.FindAssets($"t:{assetType.Name}", new[] { assetsFolderPath }));
			var assetsPaths = assetGuids.ConvertAll(a => AssetDatabase.GUIDToAssetPath(a));
			var idScript = GenerateIdsScript(assetsPaths);
			var scriptableObjectScript = GenerateScriptableObjectsScript(assetType);
			var importerScript = GenerateImporterScript(type, assetType);

			if (CacheScriptAsOld)
			{
				var obj = AssetDatabase.LoadAssetAtPath<TextAsset>(thisAssetPath);

				File.WriteAllText($"Assets/{type.Name}_Old.cs", $"/*\n{obj.text}\n*/");
			}

			File.WriteAllText($"Assets/{TIdName}.cs", idScript);
			File.WriteAllText($"Assets/{TScriptableObjectName}.cs", scriptableObjectScript);
			File.WriteAllText($"Assets/{type.Name}.cs", importerScript);
			AssetDatabase.DeleteAsset(thisAssetPath);
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();

			Debug.Log($"Finished generating the ids '{TIdName}' and importer for '{type.Name}'data type");
		}

		private string GenerateIdsScript(IList<string> ids)
		{
			var stringBuilder = new StringBuilder();

			stringBuilder.AppendLine("using System.Collections.Generic;");
			stringBuilder.AppendLine("using System.Collections.ObjectModel;");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("/* AUTO GENERATED CODE */");
			stringBuilder.AppendLine("namespace Game.Ids");
			stringBuilder.AppendLine("{");

			stringBuilder.AppendLine($"\tpublic enum {TIdName}");
			stringBuilder.AppendLine("\t{");
			GenerateEnums(stringBuilder, ids);
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("}");

			return stringBuilder.ToString();
		}

		private void GenerateEnums(StringBuilder stringBuilder, IList<string> list)
		{
			for (var i = 0; i < list.Count; i++)
			{
				var id = list[i].Substring(list[i].LastIndexOf('/') + 1);

				stringBuilder.Append("\t\t");
				stringBuilder.Append(id.Substring(0, id.IndexOf('.')).Replace(' ', '_'));
				stringBuilder.Append(i + 1 == list.Count ? "\n" : ",\n");
			}
		}

		private string GenerateScriptableObjectsScript(Type assetType)
		{
			var stringBuilder = new StringBuilder();

			stringBuilder.AppendLine("using GameLovers.Services.AssetsImporter;");
			stringBuilder.AppendLine("using Game.Ids;");
			stringBuilder.AppendLine("using UnityEngine;");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("/* AUTO GENERATED CODE */");
			stringBuilder.AppendLine("namespace Game.Configs");
			stringBuilder.AppendLine("{");

			stringBuilder.AppendLine($"\tpublic class {TScriptableObjectName} : AssetConfigsScriptableObject<{TIdName}, {assetType.Name}>");
			stringBuilder.AppendLine("\t{");
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("}");

			return stringBuilder.ToString();
		}

		private string GenerateImporterScript(Type importerType, Type assetType)
		{
			var stringBuilder = new StringBuilder();

			stringBuilder.AppendLine("using Game.Configs;");
			stringBuilder.AppendLine("using Game.Ids;");
			stringBuilder.AppendLine("using UnityEngine;");
			stringBuilder.AppendLine("");
			stringBuilder.AppendLine("/* AUTO GENERATED CODE */");
			stringBuilder.AppendLine($"namespace {importerType.Namespace}");
			stringBuilder.AppendLine("{");

			stringBuilder.AppendLine($"\tpublic class {importerType.Name} : AssetsConfigsImporter<{TIdName}, {assetType.Name}, {TScriptableObjectName}>");
			stringBuilder.AppendLine("\t{");
			stringBuilder.AppendLine("\t}");

			stringBuilder.AppendLine("}");

			return stringBuilder.ToString();
		}
	}
}
