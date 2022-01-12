using Hitbloq.Entries;
using Hitbloq.Sources;
using Hitbloq.UI;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Collections.Generic;
using Zenject;

namespace Hitbloq.Other
{
    internal class AutomaticRegistration : IInitializable
    {
        private readonly IHttpService siraHttpService;
        private readonly HitbloqPanelController hitbloqPanelController;
        private readonly IPlatformUserModel platformUserModel;
        private readonly UserIDSource userIDSource;

        public AutomaticRegistration(IHttpService siraHttpService, HitbloqPanelController hitbloqPanelController, IPlatformUserModel platformUserModel, UserIDSource userIDSource)
        {
            this.siraHttpService = siraHttpService;
            this.hitbloqPanelController = hitbloqPanelController;
            this.platformUserModel = platformUserModel;
            this.userIDSource = userIDSource;
        }

        public async void Initialize()
        {
            // Check if user id exists, if it does this is not needed
            HitbloqUserID userID = await userIDSource.GetUserIDAsync();
            if (userID.id != -1)
            {
                // If we are in progress of registration, show it
                if (!userID.registered)
                {
                    HandleRegistrationProgress();
                }
                return;
            }

            // If a valid platform id doesnt exist, return
            UserInfo userInfo = await platformUserModel.GetUserInfo();
            if (userInfo == null)
            {
                return;
            }

            // If a valid scoresaber id doesnt exist, return
            IHttpResponse webResponse = await siraHttpService.GetAsync($"https://scoresaber.com/api/player/{userInfo.platformUserId}/full").ConfigureAwait(false);
            ScoreSaberUserInfo scoreSaberUserInfo = await Utils.ParseWebResponse<ScoreSaberUserInfo>(webResponse);
            if (scoreSaberUserInfo?.errorMessage == "Player not found")
            {
                hitbloqPanelController.PromptText = "<color=red>Please submit some scores from your ScoreSaber account.</color>";
                hitbloqPanelController.LoadingActive = false;
                return;
            }

            var content = new Dictionary<string, string>
            {
                { "url", $"https://scoresaber.com/u/{userInfo.platformUserId}"}
            };

            webResponse = await siraHttpService.PostAsync("https://hitbloq.com/api/add_user", content);
            HitbloqRegistrationEntry registrationEntry = await Utils.ParseWebResponse<HitbloqRegistrationEntry>(webResponse);

            if (registrationEntry != null && registrationEntry.status != "ratelimit")
            {
                HandleRegistrationProgress();
            }
            else
            {
                hitbloqPanelController.PromptText = "<color=red>Please register for Hitbloq on the Discord Server.</color>";
                hitbloqPanelController.LoadingActive = false;
            }
        }

        private void HandleRegistrationProgress()
        {
            hitbloqPanelController.PromptText = "Registering Hitbloq account, this may take a while...";
            hitbloqPanelController.LoadingActive = true;
            userIDSource.registrationRequested = true;
        }
    }
}
