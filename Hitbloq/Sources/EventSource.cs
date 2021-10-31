using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class EventSource
    {
        private readonly SiraClient siraClient;
        private HitbloqEvent cachedEvent;

        public EventSource(SiraClient siraClient)
        {
            this.siraClient = siraClient;
        }

        public async Task<HitbloqEvent> GetEventAsync(CancellationToken? cancellationToken = null)
        {
            if (cachedEvent == null)
            {
                try
                {
                    WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/event", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    cachedEvent = Utils.ParseWebResponse<HitbloqEvent>(webResponse);
                }
                catch (TaskCanceledException) { }
            }
            return cachedEvent;
        }
    }
}
