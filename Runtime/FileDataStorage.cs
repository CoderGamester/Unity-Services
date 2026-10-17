using System;
using System.IO;
using System.Text;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services
{
	/// <summary>
	/// Stores each key as a UTF-8 <c>{key}.json</c> file in one directory: the directory passed to the
	/// constructor, created when missing, or <see cref="Application.persistentDataPath"/> by default.
	/// Writes go to a temporary file that then replaces the previous file, so an interrupted write never
	/// leaves a partially written value behind.
	/// </summary>
	public class FileDataStorage : IDataStorage
	{
		private const string FileExtension = ".json";
		private const string TempExtension = ".tmp";

		private static readonly Encoding _utf8NoBom = new UTF8Encoding(false);
		private static readonly char[] _invalidKeyChars = Path.GetInvalidFileNameChars();

		private readonly string _directory;

		public FileDataStorage() : this(Application.persistentDataPath)
		{
		}

		public FileDataStorage(string directory)
		{
			if (string.IsNullOrEmpty(directory))
			{
				throw new ArgumentException("A storage directory is required.", nameof(directory));
			}

			_directory = directory;
			Directory.CreateDirectory(_directory);
		}

		/// <inheritdoc />
		public bool HasKey(string key)
		{
			return File.Exists(GetPath(key));
		}

		/// <inheritdoc />
		public string Read(string key)
		{
			var path = GetPath(key);

			return File.Exists(path) ? File.ReadAllText(path, _utf8NoBom) : null;
		}

		/// <inheritdoc />
		public void Write(string key, string value)
		{
			var path = GetPath(key);
			var tempPath = path + TempExtension;

			using (var stream = new FileStream(tempPath, FileMode.Create, FileAccess.Write, FileShare.None))
			{
				var bytes = _utf8NoBom.GetBytes(value ?? string.Empty);

				stream.Write(bytes, 0, bytes.Length);
				stream.Flush(true);
			}

			if (File.Exists(path))
			{
				File.Replace(tempPath, path, null);
			}
			else
			{
				File.Move(tempPath, path);
			}
		}

		/// <inheritdoc />
		public void Delete(string key)
		{
			File.Delete(GetPath(key));
		}

		private string GetPath(string key)
		{
			if (string.IsNullOrEmpty(key) || key.IndexOfAny(_invalidKeyChars) >= 0)
			{
				throw new ArgumentException($"'{key}' is not a valid file name.", nameof(key));
			}

			return Path.Combine(_directory, key + FileExtension);
		}
	}
}
