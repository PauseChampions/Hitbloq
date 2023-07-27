using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
	internal class PoolInfoSource
	{
		private readonly Dictionary<string, HitbloqPoolInfo> _cache = new();
		private readonly IHttpService _siraHttpService;

		public PoolInfoSource(IHttpService siraHttpService)
		{
			_siraHttpService = siraHttpService;
		}

		public async Task<HitbloqPoolInfo?> GetPoolInfoAsync(string poolID, CancellationToken? cancellationToken = null)
		{
			if (_cache.TryGetValue(poolID, out var cachedValue))
			{
				return cachedValue;
			}

			try
			{
				var webResponse = await _siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/ranked_list/{poolID}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
				var poolInfo = await Utils.ParseWebResponse<HitbloqPoolInfo>(webResponse);
				if (poolInfo != null)
				{
					_cache[poolID] = poolInfo;
				}

				return poolInfo;
			}
			catch (TaskCanceledException)
			{
				return null;
			}
		}
	}
}