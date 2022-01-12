using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class RankInfoSource
    {
        private readonly IHttpService siraHttpService;
        private readonly UserIDSource userIDSource;

        public RankInfoSource(IHttpService siraHttpService, UserIDSource userIDSource)
        {
            this.siraHttpService = siraHttpService;
            this.userIDSource = userIDSource;
        }

        public async Task<HitbloqRankInfo> GetRankInfoForSelfAsync(string poolID, CancellationToken? cancellationToken = null)
        {
            HitbloqUserID userID = await userIDSource.GetUserIDAsync(cancellationToken);
            if (userID.id != -1)
            {
                return await GetRankInfoAsync(poolID, userID.id, cancellationToken);
            }
            return null;
        }

        public async Task<HitbloqRankInfo> GetRankInfoAsync(string poolID, int userID, CancellationToken? cancellationToken = null)
        {
            try
            {
                IHttpResponse webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/player_rank/{poolID}/{userID}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                return await Utils.ParseWebResponse<HitbloqRankInfo>(webResponse);
            }
            catch (TaskCanceledException) { }
            return null;
        }
    }
}
