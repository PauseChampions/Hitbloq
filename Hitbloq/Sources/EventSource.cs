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
        protected override string EndpointURL => "api/event";
        
        public EventSource(IHttpService siraHttpService) : base(siraHttpService) { }
    }
}
