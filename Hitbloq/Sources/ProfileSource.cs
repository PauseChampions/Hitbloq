using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class ProfileSource
    {
        private readonly SiraClient siraClient;
        private readonly UserIDSource userIDSource;

        public ProfileSource(SiraClient siraClient, UserIDSource userIDSource)
        {
            this.siraClient = siraClient;
            this.userIDSource = userIDSource;
        }

        public async Task<HitbloqProfile> GetProfileForSelfAsync(CancellationToken? cancellationToken = null)
        {
            HitbloqUserID userID = await userIDSource.GetUserIDAsync(cancellationToken);
            if (userID != null)
            {
                return await GetProfileAsync(userID.id, cancellationToken);
            }
            return null;
        }

        public async Task<HitbloqProfile> GetProfileAsync(int userID, CancellationToken? cancellationToken = null)
        {
            try
            {
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/users/{userID}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                return Utils.ParseWebResponse<HitbloqProfile>(webResponse);
            }
            catch (TaskCanceledException) { }
            return null;
        }
    }
}
