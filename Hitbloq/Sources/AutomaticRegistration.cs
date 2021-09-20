﻿using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Collections.Specialized;
using System.Threading;
using Zenject;

namespace Hitbloq.Sources
{
    internal class AutomaticRegistration : IInitializable
    {
        private readonly SiraClient siraClient;
        private readonly IPlatformUserModel platformUserModel;
        private readonly UserIDSource userIDSource;

        public AutomaticRegistration(SiraClient siraClient, IPlatformUserModel platformUserModel, UserIDSource userIDSource)
        {
            this.siraClient = siraClient;
            this.platformUserModel = platformUserModel;
            this.userIDSource = userIDSource;
        }

        public async void Initialize()
        {
            // Check if user id exists, if it does this is not needed
            HitbloqUserID userID = await userIDSource.GetUserIDAsync();
            if (userID.id != -1)
            {
                return;
            }

            // If a valid platform id doesnt exist, return
            UserInfo userInfo = await platformUserModel.GetUserInfo();
            if (userInfo == null)
            {
                return;
            }

            // If a valid scoresaber id doesnt exist, return
            WebResponse webResponse = await siraClient.GetAsync($"https://new.scoresaber.com/api/player/{userInfo.platformUserId}/full", CancellationToken.None).ConfigureAwait(false);
            ScoreSaberUserInfo scoreSaberUserInfo = Utils.ParseWebResponse<ScoreSaberUserInfo>(webResponse);
            if (scoreSaberUserInfo?.playerInfo == null)
            {
                return;
            }

            string postData = $"url=https://scoresaber.com/u/{userInfo.platformUserId}";
            ASCIIEncoding encoding = new ASCIIEncoding();
            byte[] requestParams = encoding.GetBytes(postData);
            webResponse = await siraClient.PostAsync("https://hitbloq.com/api/add_user", requestParams, CancellationToken.None);
            HitbloqRegistrationEntry registrationEntry = Utils.ParseWebResponse<HitbloqRegistrationEntry>(webResponse);
        }
    }
}
