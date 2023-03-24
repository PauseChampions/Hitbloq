using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Pages;
using Hitbloq.Utilities;
using SiraUtil.Web;
using UnityEngine;

namespace Hitbloq.Sources
{
	internal class GlobalPoolLeaderboardSource : IPoolLeaderboardSource
	{
		private readonly IHttpService _siraHttpService;
		private Sprite? _icon;

		public GlobalPoolLeaderboardSource(IHttpService siraHttpService)
		{
			_siraHttpService = siraHttpService;
		}

		public string HoverHint => "Global";

		public Sprite Icon
		{
			get
			{
				if (_icon == null)
				{
					_icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.GlobalIcon.png");
				}

				return _icon;
			}
		}

		public async Task<PoolLeaderboardPage?> GetScoresAsync(string poolID, CancellationToken cancellationToken = default, int page = 0)
		{
			try
			{
				var webResponse = await _siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/ladder/{poolID}/players/{page}", cancellationToken: cancellationToken).ConfigureAwait(false);
				var serializablePage = await Utils.ParseWebResponse<SerializablePoolLeaderboardPage>(webResponse);
				if (serializablePage is {Ladder: { }})
				{
					return new PoolLeaderboardPage(this, serializablePage.Ladder, poolID, page);
				}
			}
			catch (TaskCanceledException)
			{
			}

			return null;
		}
	}
}