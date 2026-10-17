using System;
using System.Collections.Generic;
using Newtonsoft.Json;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services
{
	/// <summary>
	/// This interface provides the access to the player's save persistent data 
	/// </summary>
	public interface IDataProvider
	{
		/// <summary>
		/// Requests the player's data of <typeparamref name="T"/> type
		/// </summary>
		T GetData<T>() where T : class;

		/// <summary>
		/// Requests if the service has the given player's data of <typeparamref name="T"/> type
		/// </summary>
		bool HasData<T>() where T : class;
	}

	/// <summary>
	/// This service allows to manage all the persistent data in the game.
	/// Data are strictly reference types to guarantee that there is no boxing/unboxing and lost of referencing when changing it's data. 
	/// </summary>
	public interface IDataService : IDataProvider
	{
		/// <summary>
		/// Saves the game's given <typeparamref name="T"/> data to its storage
		/// </summary>
		void SaveData<T>() where T : class;

		/// <summary>
		/// Saves all game's data to its storage
		/// </summary>
		void SaveAllData();

		/// <summary>
		/// Loads the game's given <typeparamref name="T"/> data from its storage and returns it
		/// </summary>
		T LoadData<T>() where T : class;

		/// <summary>
		/// Adds or replaces the given <paramref name="data"/> to be maintained in memory.
		/// </summary>
		void AddOrReplaceData<T>(T data) where T : class;
	}

	/// <summary>
	/// Keeps game data in memory by type and persists it as JSON through an <see cref="IDataStorage"/>,
	/// <see cref="PlayerPrefsDataStorage"/> by default. Each type is stored under <c>typeof(T).Name</c>, or under
	/// <c>{keyPrefix}.{typeof(T).Name}</c> when a key prefix is given, so services with different prefixes keep
	/// separate saves in the same storage.
	/// </summary>
	public class DataService : IDataService
	{
		private readonly IDictionary<Type, object> _data = new Dictionary<Type, object>();
		private readonly IDataStorage _storage;
		private readonly string _keyPrefix;

		/// <summary>The in-memory store, keyed by data type. Editor introspection only — see AGENTS.md §4.</summary>
		internal IReadOnlyDictionary<Type, object> DataEntries => (IReadOnlyDictionary<Type, object>)_data;

		/// <summary>The storage this service persists to. Editor introspection only.</summary>
		internal IDataStorage Storage => _storage;

		public DataService() : this(new PlayerPrefsDataStorage())
		{
		}

		public DataService(IDataStorage storage, string keyPrefix = null)
		{
			_storage = storage ?? throw new ArgumentNullException(nameof(storage));
			_keyPrefix = keyPrefix;
		}

		/// <inheritdoc />
		public bool HasData<T>() where T : class
		{
			return _data.ContainsKey(typeof(T));
		}

		/// <inheritdoc />
		public T GetData<T>() where T : class
		{
			return _data[typeof(T)] as T;
		}

		/// <inheritdoc />
		public void SaveData<T>() where T : class
		{
			Save(typeof(T), _data[typeof(T)]);
		}

		/// <inheritdoc />
		public void SaveAllData()
		{
			foreach (var data in _data)
			{
				Save(data.Key, data.Value);
			}
		}

		/// <inheritdoc />
		public T LoadData<T>() where T : class
		{
			var json = _storage.Read(GetStorageKey(typeof(T)));
			var instance = string.IsNullOrEmpty(json) ? Activator.CreateInstance<T>() : JsonConvert.DeserializeObject<T>(json);

			AddOrReplaceData(instance);

			return instance;
		}

		/// <inheritdoc />
		public void AddOrReplaceData<T>(T data) where T : class
		{
			if(HasData<T>())
			{
				_data[typeof(T)] = data;
			}
			else
			{
				_data.Add(typeof(T), data);
			}
		}

		/// <summary>
		/// Returns the storage key that data of the given <paramref name="type"/> is persisted under.
		/// </summary>
		internal string GetStorageKey(Type type)
		{
			return string.IsNullOrEmpty(_keyPrefix) ? type.Name : $"{_keyPrefix}.{type.Name}";
		}

		/// <summary>
		/// Runs after a save has been written; override to mirror the data elsewhere.
		/// </summary>
		protected virtual void OnDataSaved(string key, object data, Type type)
		{
		}

		private void Save(Type type, object data)
		{
			var key = GetStorageKey(type);

			_storage.Write(key, JsonConvert.SerializeObject(data));
			OnDataSaved(key, data, type);
		}
	}
}