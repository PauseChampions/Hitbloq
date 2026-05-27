using System.Collections.Generic;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Sources;
using Hitbloq.UI;
using Hitbloq.UI.ViewControllers;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Other
{
	internal class LeaderboardRefresher
	{
		private const int RefreshCompletionMessageMilliseconds = 1000;
		private const int RefreshQueueCheckCount = 7;
		private const int RefreshQueueCheckDelayMilliseconds = 3000;
		private const string RefreshRateLimitErrorText = "refreshed too fast";
		private const string RefreshSuccessText = "<color=green>Score refreshed!</color>";

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
				_hitbloqPanelController.PromptText = "Refreshing...";

				var userID = await _userIDSource.GetUserIDAsync();

				if (userID == null)
				{
					return false;
				}

				var webResponse = await _siraHttpService.GetAsync($"https://hitbloq.com/api/update_user/{userID.ID}").ConfigureAwait(false);
				var refreshEntry = await Utils.ParseWebResponse<HitbloqRefreshEntry>(webResponse);

				if (refreshEntry is {Error: null})
				{
					// Edited by GPT-5 Codex 2026-05-27
					// Queue polling now shows retry progress after the first missed check.
					// A short success delay keeps the completed message visible before UI refresh work resumes.
					for (var i = 0; i < RefreshQueueCheckCount; i++)
					{
						await Task.Delay(RefreshQueueCheckDelayMilliseconds);

						webResponse = await _siraHttpService.GetAsync("https://hitbloq.com/api/actions").ConfigureAwait(false);
						var actionQueueEntries = await Utils.ParseWebResponse<List<HitbloqActionQueueEntry>>(webResponse);

						if (actionQueueEntries == null || !actionQueueEntries.Exists(entry => entry.ID == refreshEntry.ID))
						{
							_hitbloqPanelController.LoadingActive = false;
							_hitbloqPanelController.PromptText = RefreshSuccessText;
							_ = ClearRefreshSuccessTextAfterDelay();
							return true;
						}

						_hitbloqPanelController.PromptText = $"Refreshing... ({i + 1} of {RefreshQueueCheckCount} tries)";
					}

					_hitbloqPanelController.PromptText = "<color=red>The action queue is very busy, your score cannot be refreshed for now.</color>";
				}
				else if (refreshEntry is {Error: { }})
				{
					// Edited by GPT-5 Codex 2026-05-27
					// Automatic refresh can collide with server cooldowns during quick menu changes.
					// Treat cooldown as a quiet miss instead of showing a red error to the player.
					if (IsRefreshRateLimitError(refreshEntry.Error))
					{
						_hitbloqPanelController.LoadingActive = false;
						return false;
					}

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

		private static bool IsRefreshRateLimitError(string error)
		{
			return error.ToLowerInvariant().Contains(RefreshRateLimitErrorText);
		}

		private async Task ClearRefreshSuccessTextAfterDelay()
		{
			// Edited by GPT-5 Codex 2026-05-27
			// Success text stays visible while the caller starts leaderboard reload immediately.
			// Only clear the text if nothing else replaced it during that one-second hold.
			await Task.Delay(RefreshCompletionMessageMilliseconds);

			if (_hitbloqPanelController.PromptText == RefreshSuccessText)
			{
				_hitbloqPanelController.PromptText = "";
			}
		}

		private async Task<bool> RefreshNeeded()
		{
			var userID = await _userIDSource.GetUserIDAsync();
			if (userID == null || !userID.Registered)
			{
				return false;
			}

			if (_beatmapListener.LastPlayedBeatmapKey.HasValue)
			{
				var levelInfo = await _levelInfoSource.GetLevelInfoAsync(_beatmapListener.LastPlayedBeatmapKey.Value);
				if (levelInfo != null)
				{
					return true;
				}
			}

			return false;
		}
	}
}
