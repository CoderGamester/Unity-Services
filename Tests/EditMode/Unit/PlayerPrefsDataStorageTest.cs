using GameLovers.Services;
using NUnit.Framework;
using UnityEngine;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.Services.Tests
{
	[TestFixture]
	public class PlayerPrefsDataStorageTest
	{
		private const string Key = nameof(PlayerPrefsDataStorageTest);

		private PlayerPrefsDataStorage _storage;

		[SetUp]
		public void Init()
		{
			_storage = new PlayerPrefsDataStorage();
			PlayerPrefs.DeleteKey(Key);
		}

		[TearDown]
		public void Cleanup()
		{
			PlayerPrefs.DeleteKey(Key);
		}

		[Test]
		// ADMIT: PlayerPrefsDataStorage.Read reports a missing key as null, and Write/Delete persist through PlayerPrefs.
		// RCR: DataStorage.cs PlayerPrefsDataStorage.Read — return GetString without the HasKey guard → RED (expected null,
		// was string.Empty). 2026-10-17
		public void Write_Read_Delete_RoundTrip()
		{
			Assert.IsNull(_storage.Read(Key));

			_storage.Write(Key, "{\"Value\":1}");

			Assert.IsTrue(_storage.HasKey(Key));
			Assert.AreEqual("{\"Value\":1}", new PlayerPrefsDataStorage().Read(Key));

			_storage.Delete(Key);

			Assert.IsFalse(_storage.HasKey(Key));
			Assert.IsNull(_storage.Read(Key));
		}
	}
}
