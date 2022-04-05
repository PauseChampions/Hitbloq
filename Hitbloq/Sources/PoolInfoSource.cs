using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class PoolInfoSource
    {
        private readonly IHttpService siraHttpService;
        private readonly Dictionary<string, HitbloqPoolInfo> cache;

        public PoolInfoSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
            cache = new Dictionary<string, HitbloqPoolInfo>();
        }

        public async Task<HitbloqPoolInfo> GetPoolInfoAsync(string poolID, CancellationToken? cancellationToken = null)
        {
            if (cache.TryGetValue(poolID, out var cachedValue))
            {
                return cachedValue;
            }

            try
            {
                var webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/ranked_list/{poolID}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                var poolInfo = await Utils.ParseWebResponse<HitbloqPoolInfo>(webResponse);

                cache[poolID] = poolInfo;
                return poolInfo;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }
    }
}
