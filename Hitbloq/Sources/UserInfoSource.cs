using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class UserInfoSource
    {
        private readonly SiraClient siraClient;
        private readonly IPlatformUserModel platformUserModel;

        private HitbloqUserInfo hitbloqUserInfo;

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
                    try
                    {
                        WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/tools/ss_to_hitbloq/{userInfo.platformUserId}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                        hitbloqUserInfo = Utils.ParseWebResponse<HitbloqUserInfo>(webResponse);
                    }
                    catch (TaskCanceledException e){}
                }
            }
            return hitbloqUserInfo;
        }
    }
}
