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
        private readonly UserIDSource userIDSource;

        public RankInfoSource(SiraClient siraClient, UserIDSource userIDSource)
        {
            this.siraClient = siraClient;
            this.userIDSource = userIDSource;
        }

        public async Task<HitbloqRankInfo> GetRankInfoAsync(string poolID, CancellationToken? cancellationToken = null)
        {
            HitbloqUserID userID = await userIDSource.GetUserIDAsync(cancellationToken);
            if (userID != null)
            {
                try
                {
                    WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/player_rank/{poolID}/{userID.id}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    return Utils.ParseWebResponse<HitbloqRankInfo>(webResponse);
                }
                catch (TaskCanceledException e) { }
            }
            return null;
        }
    }
}
