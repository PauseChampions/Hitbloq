using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
    internal class ProfileSource
    {
        private readonly IHttpService siraHttpService;

        public ProfileSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
        }

        public async Task<HitbloqProfile?> GetProfileAsync(int userID, CancellationToken? cancellationToken = null)
        {
            try
            {
                var webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/users/{userID}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                return await Utils.ParseWebResponse<HitbloqProfile>(webResponse);
            }
            catch (TaskCanceledException) { }
            return null;
        }
    }
}
