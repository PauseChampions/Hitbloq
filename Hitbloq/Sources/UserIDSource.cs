using System;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
	internal class UserIDSource
	{
		private readonly PlatformUserAccessor _platformUserAccessor;
		private readonly IHttpService _siraHttpService;

		private HitbloqUserID? _hitbloqUserID;
		public bool RegistrationRequested;

		public UserIDSource(IHttpService siraHttpService, PlatformUserAccessor platformUserAccessor)
		{
			_siraHttpService = siraHttpService;
			_platformUserAccessor = platformUserAccessor;
		}

		public event Action? UserRegisteredEvent;

		public async Task<HitbloqUserID?> GetUserIDAsync(CancellationToken? cancellationToken = null)
		{
			if (_hitbloqUserID == null || RegistrationRequested)
			{
				var userInfo = await _platformUserAccessor.GetUserInfo(CancellationToken.None);
				if (userInfo != null)
				{
					try
					{
						var webResponse = await _siraHttpService.GetAsync($"https://hitbloq.com/api/tools/ss_registered/{userInfo.platformUserId}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
						_hitbloqUserID = await Utils.ParseWebResponse<HitbloqUserID>(webResponse);

						if (_hitbloqUserID is {Registered: true})
						{
							UserRegisteredEvent?.Invoke();
							RegistrationRequested = false;
						}
					}
					catch (TaskCanceledException)
					{
					}
				}
			}

			return _hitbloqUserID;
		}
	}
}
