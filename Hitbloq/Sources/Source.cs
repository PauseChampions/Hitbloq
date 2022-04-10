using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
    internal abstract class Source<T>
    {
        private T? cache;
        private IHttpService SiraHttpService { get; }
        protected abstract string EndpointURL { get; }
        
        protected Source(IHttpService siraHttpService)
        {
            SiraHttpService = siraHttpService;
        }

        public async Task<T?> GetAsync(CancellationToken cancellationToken = default)
        {
            if (cache == null)
            {
                try
                {
                    var webResponse = await SiraHttpService.GetAsync(PluginConfig.Instance.HitbloqURL + EndpointURL, cancellationToken: cancellationToken).ConfigureAwait(false);
                    cache = await Utils.ParseWebResponse<T>(webResponse);
                }   
                catch (TaskCanceledException) { }
            }
            return cache;
        }
    }
}