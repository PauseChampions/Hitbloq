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
	internal class FriendsLeaderboardSource : IMapLeaderboardSource
	{
		private readonly FriendIDSource _friendIDSource;
		private readonly IHttpService _siraHttpService;

		private readonly UserIDSource _userIDSource;

		private List<List<HitbloqMapLeaderboardEntry>>? _cachedEntries;

		public FriendsLeaderboardSource(IHttpService siraHttpService, UserIDSource userIDSource, FriendIDSource friendIDSource)
		{
			_siraHttpService = siraHttpService;
			_userIDSource = userIDSource;
			_friendIDSource = friendIDSource;
		}

		public string HoverHint => "Friends";
		
		public Task<Sprite> Icon { get; } =
			BeatSaberMarkupLanguage.Utilities.LoadSpriteFromAssemblyAsync("Hitbloq.Images.FriendsIcon.png");

		public bool Scrollable => true;

		public async Task<List<HitbloqMapLeaderboardEntry>?> GetScoresAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken cancellationToken = default, int page = 0)
		{
			if (_cachedEntries == null)
			{
				var beatmapString = Utils.DifficultyBeatmapToString(difficultyBeatmap);
				if (beatmapString == null)
				{
					return null;
				}

				var userID = await _userIDSource.GetUserIDAsync(cancellationToken);
				var friendIDs = await _friendIDSource.GetFriendIDsAsync(cancellationToken);

				if (userID == null || userID.ID == -1)
				{
					return null;
				}

				friendIDs.Add(userID.ID);

				try
				{
					var content = new Dictionary<string, int[]>
					{
						{"friends", friendIDs.ToArray()}
					};
					var webResponse = await _siraHttpService.PostAsync($"{PluginConfig.Instance.HitbloqURL}/api/leaderboard/{beatmapString}/friends_extended", content, cancellationToken).ConfigureAwait(false);
					// like an hour of debugging and we had to remove the slash from the end of the url. that was it. not pog.

					var leaderboardEntries = await Utils.ParseWebResponse<List<HitbloqMapLeaderboardEntry>>(webResponse);
					_cachedEntries = new List<List<HitbloqMapLeaderboardEntry>>();

					if (leaderboardEntries != null)
					{
						// Splitting entries into lists of 10
						var p = 0;
						_cachedEntries.Add(new List<HitbloqMapLeaderboardEntry>());
						for (var i = 0; i < leaderboardEntries.Count; i++)
						{
							if (_cachedEntries[p].Count == 10)
							{
								_cachedEntries.Add(new List<HitbloqMapLeaderboardEntry>());
								p++;
							}

							_cachedEntries[p].Add(leaderboardEntries[i]);
						}
					}
				}
				catch (TaskCanceledException)
				{
				}
			}

			return page < _cachedEntries?.Count ? _cachedEntries[page] : null;
		}

		public void ClearCache()
		{
			_cachedEntries = null;
		}
	}
}