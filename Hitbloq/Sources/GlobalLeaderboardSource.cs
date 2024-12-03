using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using UnityEngine;

namespace Hitbloq.Sources
{
	internal class GlobalLeaderboardSource : IMapLeaderboardSource
	{
		private readonly List<List<HitbloqMapLeaderboardEntry>> _cachedEntries = new();
		private readonly IHttpService _siraHttpService;

		public GlobalLeaderboardSource(IHttpService siraHttpService)
		{
			_siraHttpService = siraHttpService;
		}

		public string HoverHint => "Global";
		
		public Task<Sprite> Icon { get; } =
			BeatSaberMarkupLanguage.Utilities.LoadSpriteFromAssemblyAsync("Hitbloq.Images.GlobalIcon.png");

		public bool Scrollable => true;

		public async Task<List<HitbloqMapLeaderboardEntry>?> GetScoresAsync(BeatmapKey difficultyBeatmap, CancellationToken cancellationToken = default, int page = 0)
		{
			if (_cachedEntries.Count < page + 1)
			{
				var beatmapString = Utils.DifficultyBeatmapToString(difficultyBeatmap);
				if (beatmapString == null)
				{
					return null;
				}

				try
				{
					var webResponse = await _siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/leaderboard/{beatmapString}/scores_extended/{page}", cancellationToken: cancellationToken).ConfigureAwait(false);
					var scores = await Utils.ParseWebResponse<List<HitbloqMapLeaderboardEntry>>(webResponse);
					if (scores != null)
					{
						_cachedEntries.Add(scores);
					}
				}
				catch (TaskCanceledException)
				{
				}
			}

			return page < _cachedEntries.Count ? _cachedEntries[page] : null;
		}

		public void ClearCache()
		{
			_cachedEntries.Clear();
		}
	}
}