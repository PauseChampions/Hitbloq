using Hitbloq.Entries;
using Hitbloq.Interfaces;
using SiraUtil.Web;

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