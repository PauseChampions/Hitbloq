using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Sources;

namespace Hitbloq.Pages
{
    internal class RankedListDetailedPage : Page<RankedListDetailedSource, HitbloqRankedListDetailedEntry>
    {
        private readonly string poolID;
        private readonly int page;
        public bool ExhaustedPages { get; private set; }
        protected override RankedListDetailedSource Source { get; }
        public override IReadOnlyList<HitbloqRankedListDetailedEntry> Entries { get; }

        public RankedListDetailedPage(RankedListDetailedSource source, IReadOnlyList<HitbloqRankedListDetailedEntry> entries, string poolID, int page)
        {
            Source = source;
            Entries = entries;
            this.poolID = poolID;
            this.page = page;
        }

        public Task<RankedListDetailedPage?> Previous(CancellationToken cancellationToken = default) => Source.GetRankedListAsync(poolID, cancellationToken, page - 1);

        public async Task<RankedListDetailedPage?> Next(CancellationToken cancellationToken = default)
        {
            var rankedList = await Source.GetRankedListAsync(poolID, cancellationToken, page + 1);
            if (rankedList != null)
            {
                return rankedList;
            }

            ExhaustedPages = true;
            return null;
        }
    }
}