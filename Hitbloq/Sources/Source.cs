using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
    public abstract class Source<T>
    {
        private IHttpService SiraHttpService { get; }
        protected abstract string EndpointURL { get; }
        
        protected Source(IHttpService siraHttpService)
        {
            SiraHttpService = siraHttpService;
        }

        public virtual async Task<T?> GetAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var webResponse = await SiraHttpService.GetAsync(PluginConfig.Instance.HitbloqURL + EndpointURL, cancellationToken: cancellationToken).ConfigureAwait(false);
                return await Utils.ParseWebResponse<T>(webResponse);
            }
            catch (TaskCanceledException) { }
            return default;
        }
    }
}