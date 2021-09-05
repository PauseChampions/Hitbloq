using Hitbloq.Entries;
using Hitbloq.Utilities;
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
                try
                {
                    WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/player_rank/{poolID}/{userInfo.id}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    return Utils.ParseWebResponse<HitbloqRankInfo>(webResponse);
                }
                catch (TaskCanceledException e) { }
            }
            return null;
        }
    }
}
