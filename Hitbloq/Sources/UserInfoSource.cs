using Hitbloq.Entries;
using SiraUtil;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class UserInfoSource
    {
        private readonly SiraClient siraClient;
        private readonly IPlatformUserModel platformUserModel;

        private HitbloqUserInfo? hitbloqUserInfo;

        public UserInfoSource(SiraClient siraClient, IPlatformUserModel platformUserModel)
        {
            this.siraClient = siraClient;
            this.platformUserModel = platformUserModel;
        }

        public async Task<HitbloqUserInfo> GetUserInfoAsync(CancellationToken? cancellationToken = null)
        {
            if (hitbloqUserInfo == null)
            {
                UserInfo userInfo = await platformUserModel.GetUserInfo();
                if (userInfo != null)
                {
                    WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/tools/ss_to_hitbloq/{userInfo.platformUserId}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    if (webResponse.IsSuccessStatusCode)
                    {
                        hitbloqUserInfo = webResponse.ContentToJson<HitbloqUserInfo>();
                    }
                }
            }
            return hitbloqUserInfo;
        }
    }
}
