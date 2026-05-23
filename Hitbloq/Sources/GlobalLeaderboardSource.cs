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
		private readonly Dictionary<int, List<HitbloqMapLeaderboardEntry>> _cachedEntries = new();
		private readonly IHttpService _siraHttpService;

		public GlobalLeaderboardSource(IHttpService siraHttpService)
		{
			_siraHttpService = siraHttpService;
		}

		public string HoverHint => "Global";
		
		public Task<Sprite> Icon { get; } =
			BSMLCompat.LoadSpriteFromAssemblyAsync("Hitbloq.Images.GlobalIcon.png");

		public bool Scrollable => true;

		public async Task<List<HitbloqMapLeaderboardEntry>?> GetScoresAsync(BeatmapKey beatmapKey, CancellationToken cancellationToken = default, int page = 0)
		{
			if (!_cachedEntries.ContainsKey(page))
			{
				var beatmapString = Utils.BeatmapKeyToString(beatmapKey);
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
						_cachedEntries[page] = scores;
					}
				}
				catch (TaskCanceledException)
				{
				}
			}

			return _cachedEntries.TryGetValue(page, out var cachedEntries) ? cachedEntries : null;
		}

		public void ClearCache()
		{
			_cachedEntries.Clear();
		}
	}
}
