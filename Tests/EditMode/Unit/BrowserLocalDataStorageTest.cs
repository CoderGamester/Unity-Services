using System;
using GameLovers.Services;
using NUnit.Framework;

// ReSharper disable once CheckNamespace

namespace GameLoversEditor.Services.Tests
{
	[TestFixture]
	public class BrowserLocalDataStorageTest
	{
		[Test]
		// ADMIT: BrowserLocalDataStorage refuses construction outside a WebGL player instead of silently storing nothing.
		// RCR: BrowserLocalDataStorage.cs LocalStorageIsAvailable — return true in the non-WebGL stub → RED (nothing thrown).
		// 2026-10-17
		public void Constructor_InEditor_ThrowsPlatformNotSupportedException()
		{
			Assert.Throws<PlatformNotSupportedException>(() => new BrowserLocalDataStorage());
		}
	}
}
