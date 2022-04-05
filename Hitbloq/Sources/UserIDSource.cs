using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class UserIDSource
    {
        private readonly IHttpService siraHttpService;
        private readonly IPlatformUserModel platformUserModel;

        private HitbloqUserID hitbloqUserID;
        public bool registrationRequested;
        public event Action UserRegisteredEvent;

        public UserIDSource(IHttpService siraHttpService, IPlatformUserModel platformUserModel)
        {
            this.siraHttpService = siraHttpService;
            this.platformUserModel = platformUserModel;
        }

        public async Task<HitbloqUserID> GetUserIDAsync(CancellationToken? cancellationToken = null)
        {
            if (hitbloqUserID == null || registrationRequested)
            {
                var userInfo = await platformUserModel.GetUserInfo();
                if (userInfo != null)
                {
                    try
                    {
                        var webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/tools/ss_registered/{userInfo.platformUserId}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                        hitbloqUserID = await Utils.ParseWebResponse<HitbloqUserID>(webResponse);

                        if (hitbloqUserID.Registered)
                        {
                            UserRegisteredEvent?.Invoke();
                            registrationRequested = false;
                        }
                    }
                    catch (TaskCanceledException) { }
                }
            }

            if (hitbloqUserID == null)
            {
                return new HitbloqUserID();
            }

            return hitbloqUserID;
        }
    }
}
