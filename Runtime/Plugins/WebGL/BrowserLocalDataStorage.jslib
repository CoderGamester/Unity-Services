// Native side of BrowserLocalDataStorage. Every call is synchronous; localStorage throws when the page
// blocks storage or the quota is full, so each access is guarded and reported to C# as a status.
mergeInto(LibraryManager.library, {
	GameLoversLocalStorageIsAvailable: function () {
		try {
			var probe = "__gamelovers_probe__";
			window.localStorage.setItem(probe, probe);
			window.localStorage.removeItem(probe);
			return 1;
		} catch (e) {
			return 0;
		}
	},

	GameLoversLocalStorageHasKey: function (key) {
		return window.localStorage.getItem(UTF8ToString(key)) !== null ? 1 : 0;
	},

	GameLoversLocalStorageRead: function (key) {
		var value = window.localStorage.getItem(UTF8ToString(key));

		if (value === null) {
			return 0;
		}

		var size = lengthBytesUTF8(value) + 1;
		var buffer = _malloc(size);
		stringToUTF8(value, buffer, size);
		return buffer;
	},

	GameLoversLocalStorageWrite: function (key, value) {
		try {
			window.localStorage.setItem(UTF8ToString(key), UTF8ToString(value));
			return 1;
		} catch (e) {
			return 0;
		}
	},

	GameLoversLocalStorageDelete: function (key) {
		window.localStorage.removeItem(UTF8ToString(key));
	}
});
