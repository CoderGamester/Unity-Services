// ReSharper disable once CheckNamespace

namespace GameLovers.Services
{
	/// <summary>
	/// A persistent string store that <see cref="DataService"/> reads and writes serialized data through.
	/// Implement it to persist data somewhere other than the shipped backends.
	/// </summary>
	public interface IDataStorage
	{
		/// <summary>
		/// Requests if a value is stored under the given <paramref name="key"/>
		/// </summary>
		/// <param name="key">The storage key.</param>
		/// <returns>True when a value is stored under <paramref name="key"/>.</returns>
		bool HasKey(string key);

		/// <summary>
		/// Reads the value stored under the given <paramref name="key"/>
		/// </summary>
		/// <param name="key">The storage key.</param>
		/// <returns>The stored value, or null when nothing is stored under <paramref name="key"/>.</returns>
		string Read(string key);

		/// <summary>
		/// Writes the given <paramref name="value"/> under the given <paramref name="key"/>, replacing any previous value.
		/// The value is persisted when the call returns.
		/// </summary>
		/// <param name="key">The storage key.</param>
		/// <param name="value">The value to store.</param>
		void Write(string key, string value);

		/// <summary>
		/// Deletes the value stored under the given <paramref name="key"/>. Does nothing when no value is stored.
		/// </summary>
		/// <param name="key">The storage key.</param>
		void Delete(string key);
	}
}
