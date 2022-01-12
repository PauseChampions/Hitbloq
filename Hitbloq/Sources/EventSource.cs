using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class EventSource
    {
        private readonly IHttpService siraHttpService;
        private HitbloqEvent cachedEvent;

        public EventSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
        }

        public async Task<HitbloqEvent> GetEventAsync(CancellationToken? cancellationToken = null)
        {
            if (cachedEvent == null)
            {
                try
                {
                    IHttpResponse webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/event", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    cachedEvent = await Utils.ParseWebResponse<HitbloqEvent>(webResponse);
                }
                catch (TaskCanceledException) { }
            }

            if (cachedEvent == null)
            {
                return new HitbloqEvent();
            }

            return cachedEvent;
        }
    }
}
