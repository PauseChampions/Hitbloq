using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class PoolInfoSource
    {
        private readonly SiraClient siraClient;
        private readonly Dictionary<string, HitbloqPoolInfo> cache;


        public PoolInfoSource(SiraClient siraClient)
        {
            this.siraClient = siraClient;
            cache = new Dictionary<string, HitbloqPoolInfo>();
        }

        public async Task<HitbloqPoolInfo> GetPoolInfoAsync(string poolID, CancellationToken? cancellationToken = null)
        {
            if (cache.TryGetValue(poolID, out HitbloqPoolInfo cachedValue))
            {
                return cachedValue;
            }

            try
            {
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/ranked_list/{poolID}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                HitbloqPoolInfo poolInfo = Utils.ParseWebResponse<HitbloqPoolInfo>(webResponse);

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
