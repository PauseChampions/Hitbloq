using System.Collections.Generic;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Sources;
using Hitbloq.UI;
using Hitbloq.UI.ViewControllers;
using Hitbloq.Utilities;
using SiraUtil.Web;
using Zenject;

namespace Hitbloq.Other
{
	internal class AutomaticRegistration : IInitializable
	{
		private readonly HitbloqPanelController _hitbloqPanelController;
		private readonly IPlatformUserModel _platformUserModel;
		private readonly IHttpService _siraHttpService;
		private readonly UserIDSource _userIDSource;

		public AutomaticRegistration(IHttpService siraHttpService, HitbloqPanelController hitbloqPanelController, IPlatformUserModel platformUserModel, UserIDSource userIDSource)
		{
			_siraHttpService = siraHttpService;
			_hitbloqPanelController = hitbloqPanelController;
			_platformUserModel = platformUserModel;
			_userIDSource = userIDSource;
		}

		public void Initialize()
		{
			_ = InitializeAsync();
		}

		private async Task InitializeAsync()
		{
			// Check if user id exists, if it does this is not needed
			var userID = await _userIDSource.GetUserIDAsync();
			if (userID == null || userID.ID != -1)
			{
				// If we are in progress of registration, show it
				if (userID is {Registered: false})
				{
					HandleRegistrationProgress();
				}

				return;
			}

			// If a valid platform id doesn't exist, return
			var userInfo = await _platformUserModel.GetUserInfo();
			if (userInfo == null)
			{
				return;
			}

			// If a valid ScoreSaber or BeatLeader id doesn't exist, return
			if (!Utils.IsDependencyLeaderboardInstalled)
			{
				return;
			}

			bool result;
			Dictionary<string, string> content;
			if (Utils.IsScoreSaberInstalled)
			{
				result = await CheckScoreSaberIdExists(userInfo).ConfigureAwait(false);
				content = new Dictionary<string, string>
				{
					{"url", $"https://scoresaber.com/u/{userInfo.platformUserId}"}
				};
			}
			else
			{
				result = await CheckBeatLeaderIdExists(userInfo).ConfigureAwait(false);
				content = new Dictionary<string, string>
				{
					{"url", $"https://www.beatleader.xyz/u/{userInfo.platformUserId}"}
				};
			}

			if (!result)
			{
				return;
			}

			var webResponse = await _siraHttpService.PostAsync("https://hitbloq.com/api/add_user", content);
			var registrationEntry = await Utils.ParseWebResponse<HitbloqRegistrationEntry>(webResponse);

			if (registrationEntry != null && registrationEntry.Status != "ratelimit")
			{
				HandleRegistrationProgress();
			}
			else
			{
				_hitbloqPanelController.PromptText = "<color=red>Please register for Hitbloq on the Discord Server.</color>";
				_hitbloqPanelController.LoadingActive = false;
			}
		}

		private async Task<bool> CheckScoreSaberIdExists(UserInfo userInfo)
		{
			var webResponse = await _siraHttpService.GetAsync($"https://scoresaber.com/api/player/{userInfo.platformUserId}/full").ConfigureAwait(false);
			var scoreSaberUserInfo = await Utils.ParseWebResponse<ScoreSaberUserInfo>(webResponse);
			if (scoreSaberUserInfo?.ErrorMessage == "Player not found")
			{
				_hitbloqPanelController.PromptText = "<color=red>Please submit some scores to your ScoreSaber account.</color>";
				_hitbloqPanelController.LoadingActive = false;
				return false;
			}

			return true;
		}

		private async Task<bool> CheckBeatLeaderIdExists(UserInfo userInfo)
		{
			var webResponse = await _siraHttpService.GetAsync($"https://api.beatleader.xyz/player/{userInfo.platformUserId}").ConfigureAwait(false);
			if (webResponse.Code is 404 or 400)
			{
				_hitbloqPanelController.PromptText = "<color=red>Please submit some scores to your BeatLeader account.</color>";
				_hitbloqPanelController.LoadingActive = false;
				return false;
			}

			return true;
		}

		private void HandleRegistrationProgress()
		{
			_hitbloqPanelController.PromptText = "Registering Hitbloq account, this may take a while...";
			_hitbloqPanelController.LoadingActive = true;
			_userIDSource.RegistrationRequested = true;
		}
	}
}