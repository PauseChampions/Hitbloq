using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Sources;

namespace Hitbloq.Pages
{
	internal class RankedListDetailedPage : Page<RankedListDetailedSource, HitbloqRankedListDetailedEntry>
	{
		private readonly int _page;
		private readonly string _poolID;

		public RankedListDetailedPage(RankedListDetailedSource source, IReadOnlyList<HitbloqRankedListDetailedEntry> entries, string poolID, int page)
		{
			Source = source;
			Entries = entries;
			_poolID = poolID;
			_page = page;
		}

		public override bool ExhaustedPages { get; protected set; }
		protected override RankedListDetailedSource Source { get; }
		public override IReadOnlyList<HitbloqRankedListDetailedEntry> Entries { get; }

		public Task<RankedListDetailedPage?> Previous(CancellationToken cancellationToken = default)
		{
			return Source.GetRankedListAsync(_poolID, cancellationToken, _page - 1);
		}

		public async Task<RankedListDetailedPage?> Next(CancellationToken cancellationToken = default)
		{
			var rankedList = await Source.GetRankedListAsync(_poolID, cancellationToken, _page + 1);
			if (rankedList != null)
			{
				return rankedList;
			}

			ExhaustedPages = true;
			return null;
		}
	}
}