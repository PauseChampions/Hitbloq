using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
#if HITBLOQ_BS_1_43_0
using OculusStudios.Platform.Core;
#endif

namespace Hitbloq.Utilities
{
	internal class PlatformUserAccessor
	{
#if HITBLOQ_BS_1_43_0
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
#elif HITBLOQ_BS_1_29_1
		private readonly IPlatformUserModel _platformUserModel;

		public PlatformUserAccessor(IPlatformUserModel platformUserModel)
		{
			_platformUserModel = platformUserModel;
		}

		public async Task<UserInfo?> GetUserInfo(CancellationToken cancellationToken = default)
		{
			cancellationToken.ThrowIfCancellationRequested();
			return await _platformUserModel.GetUserInfo();
		}

		public async Task<IReadOnlyCollection<string>?> GetUserFriendsUserIds(bool includeSteamFriends)
		{
			var userIds = await _platformUserModel.GetUserFriendsUserIds(includeSteamFriends);
			return userIds is IReadOnlyCollection<string> collection ? collection : new List<string>(userIds);
		}
#else
		private readonly IPlatformUserModel _platformUserModel;

		public PlatformUserAccessor(IPlatformUserModel platformUserModel)
		{
			_platformUserModel = platformUserModel;
		}

		public async Task<UserInfo?> GetUserInfo(CancellationToken cancellationToken = default)
		{
			var user = await _platformUserModel.GetUserInfo(cancellationToken);
			if (user == null || string.IsNullOrEmpty(user.platformUserId))
			{
				return null;
			}

			return user;
		}

		public async Task<IReadOnlyCollection<string>?> GetUserFriendsUserIds(bool includeSteamFriends)
		{
			var userIds = await _platformUserModel.GetUserFriendsUserIds(includeSteamFriends);
			return userIds is IReadOnlyCollection<string> collection ? collection : new List<string>(userIds);
		}
#endif
	}
}
