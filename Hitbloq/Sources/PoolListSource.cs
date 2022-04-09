using System.Collections.Generic;
using Hitbloq.Entries;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
    internal class PoolListSource : Source<List<HitbloqPoolListEntry>>
    {
        protected override string EndpointURL => "api/map_pools_detailed";

        public PoolListSource(IHttpService siraHttpService) : base(siraHttpService) { }
    }
}