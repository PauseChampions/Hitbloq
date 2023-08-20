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
		private readonly Dictionary<IDifficultyBeatmap, HitbloqLevelInfo?> _cache = new();
		private readonly IHttpService _siraHttpService;

		public LevelInfoSource(IHttpService siraHttpService)
		{
			_siraHttpService = siraHttpService;
		}

		public async Task<HitbloqLevelInfo?> GetLevelInfoAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken cancellationToken = default)
		{
			if (_cache.TryGetValue(difficultyBeatmap, out var cachedValue))
			{
                return cachedValue;
			}

			var beatmapString = Utils.DifficultyBeatmapToString(difficultyBeatmap);
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

				_cache[difficultyBeatmap] = levelInfo;
				return levelInfo;
			}
			catch (TaskCanceledException)
			{
				return null;
			}
		}
	}
}