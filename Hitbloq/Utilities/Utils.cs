using System.IO;
using System.Text;
using System.Threading.Tasks;
using IPA.Loader;
using Newtonsoft.Json;
using SiraUtil.Web;
using SongCore;
using Version = Hive.Versioning.Version;

namespace Hitbloq.Utilities
{
	internal static class Utils
	{
		private static bool? _isScoreSaberInstalled;
		private static bool? _isBeatLeaderInstalled;

		public static bool IsScoreSaberInstalled
		{
			get
			{
				_isScoreSaberInstalled ??= PluginManager.GetPluginFromId("ScoreSaber") is not null;
				return (bool) _isScoreSaberInstalled;
			}
		}

		public static bool IsBeatLeaderInstalled
		{
			get
			{
				if (_isBeatLeaderInstalled is null)
				{
					var plugin = PluginManager.GetPluginFromId("BeatLeader");
					_isBeatLeaderInstalled = plugin is not null && plugin.HVersion >= new Version(0, 6, 1);
				}
				
				return (bool) _isBeatLeaderInstalled;
			}
		}

		public static bool IsDependencyLeaderboardInstalled => IsScoreSaberInstalled || IsBeatLeaderInstalled;

		public static string? BeatmapKeyToString(BeatmapKey beatmapKey)
		{
			if (beatmapKey.levelId.StartsWith("custom_level_"))
			{
				var hash = Collections.hashForLevelID(beatmapKey.levelId);
				var difficulty = beatmapKey.difficulty.ToString();
				var characteristic = beatmapKey.beatmapCharacteristic.serializedName;
				return $"{hash}%7C_{difficulty}_Solo{characteristic}";
			}

			return null;
		}

		public static async Task<T?> ParseWebResponse<T>(IHttpResponse webResponse)
		{
			if (webResponse.Successful && (await webResponse.ReadAsByteArrayAsync()).Length > 3)
			{
				using var streamReader = new StreamReader(await webResponse.ReadAsStreamAsync());
				using var jsonTextReader = new JsonTextReader(streamReader);
				var jsonSerializer = new JsonSerializer();
				return jsonSerializer.Deserialize<T>(jsonTextReader);
			}

			if (!webResponse.Successful)
			{
				// Plugin.Log.Error($"Unsuccessful web response for parsing {typeof(T)}. Status code: {webResponse.Code}");
			}
			
			return default;
		}

		public static bool DoesNotHaveAlphaNumericCharacters(this string str)
		{
			var sb = new StringBuilder();
			foreach (var c in str)
			{
				if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
				{
					sb.Append(c);
				}
			}

			return sb.Length == 0;
		}

		public static string RemoveSpecialCharacters(this string str)
		{
			var sb = new StringBuilder();
			foreach (var c in str)
			{
				if (c <= 255)
				{
					sb.Append(c);
				}
			}

			return sb.ToString();
		}

		public static LevelSelectionFlowCoordinator.State GetStateForPlaylist(BeatmapLevelPack beatmapLevelPack)
		{
			var state = new LevelSelectionFlowCoordinator.State(beatmapLevelPack);
			Accessors.LevelCategoryAccessor(ref state) = SelectLevelCategoryViewController.LevelCategory.CustomSongs;
			return state;
		}
	}
}