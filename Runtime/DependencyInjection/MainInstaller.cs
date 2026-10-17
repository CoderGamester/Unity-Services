using System;

// ReSharper disable once CheckNamespace

namespace GameLovers.Services
{
	/// <inheritdoc cref="IInstaller"/>
	/// <remarks>
	/// Use this installer for generic Binding interfaces that are available in the entire scope of the game
	/// </remarks>
	public static class MainInstaller
	{
		private static readonly Installer _installer = new Installer();

		/// <summary>
		/// The single installer this static facade wraps. Editor introspection only — see AGENTS.md §4.
		/// </summary>
		internal static Installer InstallerInstance => _installer;

		/// <inheritdoc cref="IInstaller.Bind{T}"/>
		public static void Bind<T>(T instance) where T : class
		{
			_installer.Bind(instance);
		}

		/// <inheritdoc cref="IInstaller.TryResolve{T}"/>
		public static bool TryResolve<T>(out T instance)
		{
			return _installer.TryResolve(out instance);
		}

		/// <inheritdoc cref="IInstaller.Resolve{T}"/>
		public static T Resolve<T>()
		{
			return _installer.Resolve<T>();
		}

		/// <inheritdoc cref="IInstaller.Clean"/>
		public static bool Clean<T>() where T : class
		{
			return _installer.Clean<T>();
		}

		/// <inheritdoc cref="IInstaller.Clean"/>
		public static bool CleanDispose<T>() where T : class, IDisposable
		{
			_installer.Resolve<T>().Dispose();

			return _installer.Clean<T>();
		}

		/// <inheritdoc cref="IInstaller.Clean"/>
		public static void Clean()
		{
			_installer.Clean();
		}
	}
}
