using UnityEngine;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services
{
	/// <summary>
	/// Stores data in <see cref="PlayerPrefs"/>. This is the default <see cref="DataService"/> storage.
	/// </summary>
	public class PlayerPrefsDataStorage : IDataStorage
	{
		/// <inheritdoc />
		public bool HasKey(string key)
		{
			return PlayerPrefs.HasKey(key);
		}

		/// <inheritdoc />
		public string Read(string key)
		{
			return PlayerPrefs.HasKey(key) ? PlayerPrefs.GetString(key) : null;
		}

		/// <inheritdoc />
		public void Write(string key, string value)
		{
			PlayerPrefs.SetString(key, value);
			PlayerPrefs.Save();
		}

		/// <inheritdoc />
		public void Delete(string key)
		{
			PlayerPrefs.DeleteKey(key);
			PlayerPrefs.Save();
		}
	}
}
