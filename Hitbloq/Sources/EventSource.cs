using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Hitbloq.Sources
{
    internal class EventSource : Source<HitbloqEvent>
    {
        private HitbloqEvent? cachedEvent;
        protected override string EndpointURL => "api/event";
        
        public EventSource(IHttpService siraHttpService) : base(siraHttpService) { }

        public override async Task<HitbloqEvent?> GetAsync(CancellationToken cancellationToken = default)
        {
            if (cachedEvent == null)
            {
                cachedEvent = await base.GetAsync(cancellationToken);
            }

            return cachedEvent;
        }
    }
}
