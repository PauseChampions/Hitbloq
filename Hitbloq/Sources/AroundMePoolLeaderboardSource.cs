using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Pages;
using Hitbloq.Utilities;
using SiraUtil.Web;
using UnityEngine;

namespace Hitbloq.Sources
{
	internal class AroundMePoolLeaderboardSource : IPoolLeaderboardSource
	{
		private readonly IHttpService _siraHttpService;
		private readonly UserIDSource _userIDSource;

		public AroundMePoolLeaderboardSource(IHttpService siraHttpService, UserIDSource userIDSource)
		{
			_siraHttpService = siraHttpService;
			_userIDSource = userIDSource;
		}

		public string HoverHint => "Around Me";
		
		
		public Task<Sprite> Icon { get; } =
			BeatSaberMarkupLanguage.Utilities.LoadSpriteFromAssemblyAsync("Hitbloq.Images.PlayerIcon.png");

		public async Task<PoolLeaderboardPage?> GetScoresAsync(string poolID, CancellationToken cancellationToken = default, int page = 0)
		{
			var userID = await _userIDSource.GetUserIDAsync(cancellationToken);
			if (userID == null || userID.ID == -1)
			{
				return null;
			}

			var id = userID.ID;

			try
			{
				var webResponse = await _siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/ladder/{poolID}/nearby_players/{id}", cancellationToken: cancellationToken).ConfigureAwait(false);
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