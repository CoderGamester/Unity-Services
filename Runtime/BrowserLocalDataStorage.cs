using System;
using System.IO;
using System.Runtime.InteropServices;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services
{
	/// <summary>
	/// Stores data in the browser's <c>localStorage</c> in WebGL players. <c>localStorage</c> is scoped to the page
	/// origin, not the build, so saved data survives new uploads on hosts that change the build path every upload.
	/// Writes are synchronous. Construction throws <see cref="PlatformNotSupportedException"/> outside a WebGL
	/// player, including in the Editor, and <see cref="InvalidOperationException"/> when the browser blocks
	/// <c>localStorage</c>.
	/// </summary>
	public class BrowserLocalDataStorage : IDataStorage
	{
		public BrowserLocalDataStorage()
		{
			if (!LocalStorageIsAvailable())
			{
				throw new InvalidOperationException("The browser blocks localStorage for this page.");
			}
		}

		/// <inheritdoc />
		public bool HasKey(string key)
		{
			return LocalStorageHasKey(key);
		}

		/// <inheritdoc />
		public string Read(string key)
		{
			return LocalStorageRead(key);
		}

		/// <inheritdoc />
		public void Write(string key, string value)
		{
			if (!LocalStorageWrite(key, value ?? string.Empty))
			{
				throw new IOException($"localStorage rejected the write for '{key}', for example because it is full.");
			}
		}

		/// <inheritdoc />
		public void Delete(string key)
		{
			LocalStorageDelete(key);
		}

#if UNITY_WEBGL && !UNITY_EDITOR
		[DllImport("__Internal", EntryPoint = "GameLoversLocalStorageIsAvailable")]
		private static extern bool LocalStorageIsAvailable();

		[DllImport("__Internal", EntryPoint = "GameLoversLocalStorageHasKey")]
		private static extern bool LocalStorageHasKey(string key);

		[DllImport("__Internal", EntryPoint = "GameLoversLocalStorageRead")]
		private static extern string LocalStorageRead(string key);

		[DllImport("__Internal", EntryPoint = "GameLoversLocalStorageWrite")]
		private static extern bool LocalStorageWrite(string key, string value);

		[DllImport("__Internal", EntryPoint = "GameLoversLocalStorageDelete")]
		private static extern void LocalStorageDelete(string key);
#else
		// Outside a WebGL player construction fails here, so the other stubs are never reached.
		private static bool LocalStorageIsAvailable() => throw new PlatformNotSupportedException(
			$"{nameof(BrowserLocalDataStorage)} only runs in WebGL players. Use another {nameof(IDataStorage)} " +
			"in the Editor and on other platforms.");

		private static bool LocalStorageHasKey(string key) => throw new PlatformNotSupportedException();
		private static string LocalStorageRead(string key) => throw new PlatformNotSupportedException();
		private static bool LocalStorageWrite(string key, string value) => throw new PlatformNotSupportedException();
		private static void LocalStorageDelete(string key) => throw new PlatformNotSupportedException();
#endif
	}
}
