using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Pages;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
    internal class RankedListDetailedSource
    {
        private readonly IHttpService siraHttpService;

        public RankedListDetailedSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
        }

        public async Task<RankedListDetailedPage?> GetRankedListAsync(string poolID, CancellationToken cancellationToken = default, int page = 0)
        {
            try
            {
                var webResponse = await siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/ranked_list_detailed/{poolID}/{page}", cancellationToken: cancellationToken).ConfigureAwait(false);
                var rankedList = await Utils.ParseWebResponse<List<HitbloqRankedListDetailedEntry>>(webResponse);
                if (rankedList != null)
                {
                    return new RankedListDetailedPage(this, rankedList, poolID, page);
                }
            }
            catch (TaskCanceledException) { }
            return null;
        }
    }
}