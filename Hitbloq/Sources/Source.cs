using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
	internal abstract class Source<T>
	{
		private T? _cache;

		protected Source(IHttpService siraHttpService)
		{
			SiraHttpService = siraHttpService;
		}

		private IHttpService SiraHttpService { get; }
		protected abstract string EndpointURL { get; }

		public async Task<T?> GetAsync(CancellationToken cancellationToken = default)
		{
			if (_cache == null)
			{
				try
				{
					var webResponse = await SiraHttpService.GetAsync(PluginConfig.Instance.HitbloqURL + "/" + EndpointURL, cancellationToken: cancellationToken).ConfigureAwait(false);
					_cache = await Utils.ParseWebResponse<T>(webResponse);
				}
				catch (TaskCanceledException)
				{
				}
			}

			return _cache;
		}
	}
}