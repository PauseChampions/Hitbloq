using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
	internal class RankInfoSource
	{
		private readonly IHttpService _siraHttpService;
		private readonly UserIDSource _userIDSource;

		public RankInfoSource(IHttpService siraHttpService, UserIDSource userIDSource)
		{
			_siraHttpService = siraHttpService;
			_userIDSource = userIDSource;
		}

		public async Task<HitbloqRankInfo?> GetRankInfoForSelfAsync(string poolID, CancellationToken? cancellationToken = null)
		{
			var userID = await _userIDSource.GetUserIDAsync(cancellationToken);
			if (userID != null && userID.ID != -1)
			{
				return await GetRankInfoAsync(poolID, userID.ID, cancellationToken);
			}

			return null;
		}

		public async Task<HitbloqRankInfo?> GetRankInfoAsync(string poolID, int userID, CancellationToken? cancellationToken = null)
		{
			try
			{
				var webResponse = await _siraHttpService.GetAsync($"https://hitbloq.com/api/player_rank/{poolID}/{userID}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
				return await Utils.ParseWebResponse<HitbloqRankInfo>(webResponse);
			}
			catch (TaskCanceledException)
			{
			}

			return null;
		}
	}
}