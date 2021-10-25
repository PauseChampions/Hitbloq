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

        public ProfileSource(SiraClient siraClient)
        {
            this.siraClient = siraClient;
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
