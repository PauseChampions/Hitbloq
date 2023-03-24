using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Sources;

namespace Hitbloq.Pages
{
	internal sealed class PoolLeaderboardPage : Page<IPoolLeaderboardSource, HitbloqPoolLeaderboardEntry>
	{
		private readonly int _page;
		public readonly string PoolID;

		public PoolLeaderboardPage(IPoolLeaderboardSource source, IReadOnlyList<HitbloqPoolLeaderboardEntry> entries, string poolID, int page, bool exhaustedPages = false)
		{
			Source = source;
			Entries = entries;
			PoolID = poolID;
			_page = page;
			ExhaustedPages = exhaustedPages;
		}

		protected override IPoolLeaderboardSource Source { get; }
		public override IReadOnlyList<HitbloqPoolLeaderboardEntry> Entries { get; }
		public override bool ExhaustedPages { get; protected set; }

		public Task<PoolLeaderboardPage?> Previous(CancellationToken cancellationToken = default)
		{
			return Source.GetScoresAsync(PoolID, cancellationToken, _page - 1);
		}

		public async Task<PoolLeaderboardPage?> Next(CancellationToken cancellationToken = default)
		{
			var rankedList = await Source.GetScoresAsync(PoolID, cancellationToken, _page + 1);
			if (rankedList != null)
			{
				return rankedList;
			}

			ExhaustedPages = true;
			return null;
		}
	}
}