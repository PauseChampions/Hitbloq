using Hitbloq.Entries;
using Hitbloq.Interfaces;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
	internal class EventSource : Source<HitbloqEvent>, IEventSource
	{
		public EventSource(IHttpService siraHttpService) : base(siraHttpService)
		{
		}

		protected override string EndpointURL => "api/event";
	}
}