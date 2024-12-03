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
	internal class AroundMeLeaderboardSource : IMapLeaderboardSource
	{
		private readonly IHttpService _siraHttpService;
		private readonly UserIDSource _userIDSource;

		private List<HitbloqMapLeaderboardEntry>? _cachedEntries;

		public AroundMeLeaderboardSource(IHttpService siraHttpService, UserIDSource userIDSource)
		{
			_siraHttpService = siraHttpService;
			_userIDSource = userIDSource;
		}

		public string HoverHint => "Around Me";
		
		public Task<Sprite> Icon { get; } =
			BeatSaberMarkupLanguage.Utilities.LoadSpriteFromAssemblyAsync("Hitbloq.Images.PlayerIcon.png");

		public bool Scrollable => false;

		public async Task<List<HitbloqMapLeaderboardEntry>?> GetScoresAsync(BeatmapKey difficultyBeatmap, CancellationToken cancellationToken = default, int page = 0)
		{
			if (_cachedEntries == null)
			{
				var beatmapString = Utils.DifficultyBeatmapToString(difficultyBeatmap);
				if (beatmapString == null)
				{
					return null;
				}

				var userID = await _userIDSource.GetUserIDAsync(cancellationToken);
				if (userID == null || userID.ID == -1)
				{
					return null;
				}

				var id = userID.ID;

				try
				{
					var webResponse = await _siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/leaderboard/{beatmapString}/nearby_scores_extended/{id}", cancellationToken: cancellationToken).ConfigureAwait(false);
					_cachedEntries = await Utils.ParseWebResponse<List<HitbloqMapLeaderboardEntry>>(webResponse);
				}
				catch (TaskCanceledException)
				{
				}
			}

			return _cachedEntries;
		}

		public void ClearCache()
		{
			_cachedEntries = null;
		}
	}
}