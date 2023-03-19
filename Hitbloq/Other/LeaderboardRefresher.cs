using System.Collections.Generic;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Sources;
using Hitbloq.UI;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Other
{
	internal class LeaderboardRefresher
	{
		private readonly BeatmapListener _beatmapListener;
		private readonly HitbloqPanelController _hitbloqPanelController;
		private readonly LevelInfoSource _levelInfoSource;
		private readonly IHttpService _siraHttpService;
		private readonly UserIDSource _userIDSource;

		public LeaderboardRefresher(IHttpService siraHttpService, BeatmapListener beatmapListener, HitbloqPanelController hitbloqPanelController, UserIDSource userIDSource, LevelInfoSource levelInfoSource)
		{
			_siraHttpService = siraHttpService;
			_beatmapListener = beatmapListener;
			_hitbloqPanelController = hitbloqPanelController;
			_userIDSource = userIDSource;
			_levelInfoSource = levelInfoSource;
		}

		public async Task<bool> Refresh()
		{
			if (await RefreshNeeded())
			{
				_hitbloqPanelController.LoadingActive = true;
				_hitbloqPanelController.PromptText = "Refreshing Score...";

				var userID = await _userIDSource.GetUserIDAsync();

				if (userID == null)
				{
					return false;
				}

				var webResponse = await _siraHttpService.GetAsync($"https://hitbloq.com/api/update_user/{userID.ID}").ConfigureAwait(false);
				var refreshEntry = await Utils.ParseWebResponse<HitbloqRefreshEntry>(webResponse);

				if (refreshEntry is {Error: null})
				{
					// Try checking action queue if our action is completed, timeout at 7 times
					for (var i = 0; i < 7; i++)
					{
						await Task.Delay(3000);

						webResponse = await _siraHttpService.GetAsync("https://hitbloq.com/api/actions").ConfigureAwait(false);
						var actionQueueEntries = await Utils.ParseWebResponse<List<HitbloqActionQueueEntry>>(webResponse);

						if (actionQueueEntries == null || !actionQueueEntries.Exists(entry => entry.ID == refreshEntry.ID))
						{
							_hitbloqPanelController.LoadingActive = false;
							_hitbloqPanelController.PromptText = "<color=green>Score refreshed!</color>";
							return true;
						}
					}

					_hitbloqPanelController.PromptText = "<color=red>The action queue is very busy, your score cannot be refreshed for now.</color>";
				}
				else if (refreshEntry is {Error: { }})
				{
					_hitbloqPanelController.PromptText = $"<color=red>Error: {refreshEntry.Error}</color>";
				}
				else
				{
					_hitbloqPanelController.PromptText = "<color=red>Hitbloq servers are not responding, please try again later.</color>";
				}
			}

			_hitbloqPanelController.LoadingActive = false;
			return false;
		}

		private async Task<bool> RefreshNeeded()
		{
			var userID = await _userIDSource.GetUserIDAsync();
			if (userID == null || !userID.Registered)
			{
				return false;
			}

			if (_beatmapListener.LastPlayedDifficultyBeatmap != null)
			{
				var levelInfo = await _levelInfoSource.GetLevelInfoAsync(_beatmapListener.LastPlayedDifficultyBeatmap);
				if (levelInfo != null)
				{
					return true;
				}
			}

			return false;
		}
	}
}