using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
	internal class LevelInfoSource
	{
		private readonly Dictionary<BeatmapKey, HitbloqLevelInfo?> _cache = new();
		private readonly IHttpService _siraHttpService;

		public LevelInfoSource(IHttpService siraHttpService)
		{
			_siraHttpService = siraHttpService;
		}

		public async Task<HitbloqLevelInfo?> GetLevelInfoAsync(BeatmapKey beatmapKey, CancellationToken cancellationToken = default)
		{
			if (_cache.TryGetValue(beatmapKey, out var cachedValue))
			{
                return cachedValue;
			}

			var beatmapString = Utils.DifficultyBeatmapToString(beatmapKey);
			if (beatmapString == null)
			{
				return null;
			}

			try
			{
				var webResponse = await _siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/leaderboard/{beatmapString}/info", cancellationToken: cancellationToken).ConfigureAwait(false);
				var levelInfo = await Utils.ParseWebResponse<HitbloqLevelInfo>(webResponse);

				if (levelInfo?.Error != null)
				{
					return null;
				}

				_cache[beatmapKey] = levelInfo;
				return levelInfo;
			}
			catch (TaskCanceledException)
			{
				return null;
			}
		}
	}
}