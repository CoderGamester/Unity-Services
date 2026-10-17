using System;
using System.IO;
using GameLovers.Services;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.Services.Tests
{
	[TestFixture]
	public class FileDataStorageTest
	{
		private const string Key = "PersistentData";

		private string _directory;
		private FileDataStorage _storage;

		[SetUp]
		public void Init()
		{
			_directory = Path.Combine(Path.GetTempPath(), nameof(FileDataStorageTest), Guid.NewGuid().ToString("N"));
			_storage = new FileDataStorage(_directory);
		}

		[TearDown]
		public void Cleanup()
		{
			if (Directory.Exists(_directory))
			{
				Directory.Delete(_directory, true);
			}
		}

		[Test]
		// ADMIT: FileDataStorage persists a value to a file that a new instance over the same directory reads back,
		// and Delete removes it.
		// RCR: FileDataStorage.cs Delete — skip File.Delete → RED (HasKey still true after Delete). 2026-10-17
		public void Write_Read_Delete_RoundTripThroughNewInstance()
		{
			Assert.IsNull(_storage.Read(Key));

			_storage.Write(Key, "{\"Name\":\"Hérø\"}");

			var reopened = new FileDataStorage(_directory);

			Assert.IsTrue(reopened.HasKey(Key));
			Assert.AreEqual("{\"Name\":\"Hérø\"}", reopened.Read(Key));

			reopened.Delete(Key);

			Assert.IsFalse(_storage.HasKey(Key));
			Assert.IsNull(_storage.Read(Key));
		}

		[Test]
		// ADMIT: FileDataStorage.Write over an existing key replaces its file and leaves no temporary file behind.
		// RCR: FileDataStorage.cs Write — drop the File.Replace call for an existing file → RED (Read returns "first"). 2026-10-17
		public void Write_ExistingKey_ReplacesValueAndLeavesNoTempFile()
		{
			_storage.Write(Key, "first");
			_storage.Write(Key, "second");

			Assert.AreEqual("second", _storage.Read(Key));
			CollectionAssert.AreEquivalent(new[] { Key + ".json" },
				Array.ConvertAll(Directory.GetFileSystemEntries(_directory), Path.GetFileName));
		}

		[Test]
		// ADMIT: FileDataStorage.Write stages the value in a temporary file, so a write that fails midway leaves the
		// previous value intact instead of a truncated save.
		// RCR: FileDataStorage.cs Write — write the target file directly with File.WriteAllText → RED (no exception thrown).
		// 2026-10-17
		public void Write_TempFileFails_KeepsPreviousValue()
		{
			_storage.Write(Key, "first");
			Directory.CreateDirectory(Path.Combine(_directory, Key + ".json.tmp"));

			Assert.Catch<Exception>(() => _storage.Write(Key, "second"));
			Assert.AreEqual("first", _storage.Read(Key));
		}

		[Test]
		// ADMIT: FileDataStorage rejects keys that are not plain file names, so a key cannot address a file outside its
		// directory.
		// RCR: FileDataStorage.cs GetPath — drop the invalid file-name check → RED (no ArgumentException). 2026-10-17
		public void Write_KeyWithPathSeparator_ThrowsArgumentException()
		{
			Assert.Throws<ArgumentException>(() => _storage.Write("../" + Key, "value"));
		}
	}
}
