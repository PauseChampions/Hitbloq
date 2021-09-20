using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class UserIDSource
    {
        private readonly SiraClient siraClient;
        private readonly IPlatformUserModel platformUserModel;

        private HitbloqUserID hitbloqUserID;

        public UserIDSource(SiraClient siraClient, IPlatformUserModel platformUserModel)
        {
            this.siraClient = siraClient;
            this.platformUserModel = platformUserModel;
        }

        public async Task<HitbloqUserID> GetUserIDAsync(CancellationToken? cancellationToken = null)
        {
            if (hitbloqUserID == null)
            {
                UserInfo userInfo = await platformUserModel.GetUserInfo();
                if (userInfo != null)
                {
                    try
                    {
                        WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/tools/ss_registered/{userInfo.platformUserId}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                        hitbloqUserID = Utils.ParseWebResponse<HitbloqUserID>(webResponse);
                    }
                    catch (TaskCanceledException) { }
                }
            }
            return hitbloqUserID;
        }
    }
}
