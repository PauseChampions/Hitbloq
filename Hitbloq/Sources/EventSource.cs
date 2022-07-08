using Hitbloq.Entries;
using SiraUtil.Web;
using Hitbloq.Interfaces;

namespace Hitbloq.Sources
{
    internal class EventSource : Source<HitbloqEvent>, IEventSource
    {
        protected override string EndpointURL => "api/event";

        public EventSource(IHttpService siraHttpService) : base(siraHttpService)
        {
        }
    }
}