using Hitbloq.Entries;
using SiraUtil;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class RankInfoSource
    {
        private readonly SiraClient siraClient;
        private readonly UserInfoSource userInfoSource;

        public RankInfoSource(SiraClient siraClient, UserInfoSource userInfoSource)
        {
            this.siraClient = siraClient;
            this.userInfoSource = userInfoSource;
        }

        public async Task<HitbloqRankInfo> GetRankInfoAsync(string poolID, CancellationToken? cancellationToken = null)
        {
            HitbloqUserInfo userInfo = await userInfoSource.GetUserInfoAsync(cancellationToken);
            if (userInfo != null)
            {
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/player_rank/{poolID}/{userInfo.id}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                if (webResponse.IsSuccessStatusCode)
                {
                    return webResponse.ContentToJson<HitbloqRankInfo>();
                }
            }
            return null;
        }
    }
}
