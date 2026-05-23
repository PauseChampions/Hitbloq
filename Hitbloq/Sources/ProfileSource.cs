using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
	internal class ProfileSource
	{
		private static readonly SemaphoreSlim ProfileRequestSemaphore = new(2, 2);
		private static readonly ConcurrentDictionary<int, HitbloqProfile> CachedProfiles = new ConcurrentDictionary<int, HitbloqProfile>();
		private readonly IHttpService _siraHttpService;

		public ProfileSource(IHttpService siraHttpService)
		{
			_siraHttpService = siraHttpService;
		}

		public async Task<HitbloqProfile?> GetProfileAsync(int userID, CancellationToken? cancellationToken = null)
		{
			if (CachedProfiles.TryGetValue(userID, out var cachedProfile))
			{
				return cachedProfile;
			}

			try
			{
				var token = cancellationToken ?? CancellationToken.None;
				await ProfileRequestSemaphore.WaitAsync(token).ConfigureAwait(false);
				try
				{
					if (CachedProfiles.TryGetValue(userID, out cachedProfile))
					{
						return cachedProfile;
					}

					var webResponse = await _siraHttpService.GetAsync($"https://hitbloq.com/api/users/{userID}", cancellationToken: token).ConfigureAwait(false);
					var profile = await Utils.ParseWebResponse<HitbloqProfile>(webResponse);
					if (profile != null)
					{
						CachedProfiles.TryAdd(userID, profile);
					}

					return profile;
				}
				finally
				{
					ProfileRequestSemaphore.Release();
				}
			}
			catch (TaskCanceledException)
			{
			}

			return null;
		}
	}
}
