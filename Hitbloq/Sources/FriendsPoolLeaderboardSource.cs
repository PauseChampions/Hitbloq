using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Pages;
using Hitbloq.Utilities;
using SiraUtil.Web;
using UnityEngine;

namespace Hitbloq.Sources
{
	internal class FriendsPoolLeaderboardSource : IPoolLeaderboardSource
	{
		private readonly FriendIDSource _friendIDSource;
		private readonly IHttpService _siraHttpService;

		private readonly UserIDSource _userIDSource;
		private Sprite? _icon;

		public FriendsPoolLeaderboardSource(IHttpService siraHttpService, UserIDSource userIDSource, FriendIDSource friendIDSource)
		{
			_siraHttpService = siraHttpService;
			_userIDSource = userIDSource;
			_friendIDSource = friendIDSource;
		}

		public string HoverHint => "Friends";

		public Sprite Icon
		{
			get
			{
				if (_icon == null)
				{
					_icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.FriendsIcon.png");
				}

				return _icon;
			}
		}

		public async Task<PoolLeaderboardPage?> GetScoresAsync(string poolID, CancellationToken cancellationToken = default, int page = 0)
		{
			var userID = await _userIDSource.GetUserIDAsync(cancellationToken);
			var friendIDs = await _friendIDSource.GetFriendIDsAsync(cancellationToken);

			if (userID == null || userID.ID == -1)
			{
				return null;
			}

			friendIDs.Add(userID.ID);

			try
			{
				var content = new Dictionary<string, int[]>
				{
					{"friends", friendIDs.ToArray()}
				};
				var webResponse = await _siraHttpService.PostAsync($"{PluginConfig.Instance.HitbloqURL}/api/ladder/{poolID}/friends", content, cancellationToken).ConfigureAwait(false);
				var serializablePage = await Utils.ParseWebResponse<SerializablePoolLeaderboardPage>(webResponse);
				if (serializablePage is {Ladder: { }})
				{
					return new PoolLeaderboardPage(this, serializablePage.Ladder, poolID, page, true);
				}
			}
			catch (TaskCanceledException)
			{
			}

			return null;
		}
	}
}