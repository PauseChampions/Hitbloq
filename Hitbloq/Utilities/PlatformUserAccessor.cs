using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using OculusStudios.Platform.Core;

namespace Hitbloq.Utilities
{
	internal class PlatformUserAccessor
	{
		private readonly IPlatform _platform;

		public PlatformUserAccessor(IPlatform platform)
		{
			_platform = platform;
		}

		public Task<UserInfo?> GetUserInfo(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			var user = _platform.user;
			if (user == null || user.userId == 0)
			{
				return Task.FromResult<UserInfo?>(null);
			}

			return Task.FromResult<UserInfo?>(new UserInfo(GetPlatform(), user.userId.ToString(), user.displayName));
		}

		public Task<IReadOnlyCollection<string>?> GetUserFriendsUserIds(bool includeSteamFriends)
		{
			return Task.FromResult<IReadOnlyCollection<string>?>(null);
		}

		private UserInfo.Platform GetPlatform()
		{
			return _platform.vendor switch
			{
				Vendor.Valve => UserInfo.Platform.Steam,
				Vendor.Meta => UserInfo.Platform.Oculus,
				Vendor.Sony => UserInfo.Platform.PS5,
				_ => UserInfo.Platform.Test
			};
		}
	}
}
