using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Sources;

namespace Hitbloq.Pages
{
    internal class PoolLeaderboardPage : Page<IPoolLeaderboardSource, HitbloqPoolLeaderboardEntry>
    {
        public readonly string poolID;
        private readonly int page;
        
        protected override IPoolLeaderboardSource Source { get; }
        public override IReadOnlyList<HitbloqPoolLeaderboardEntry> Entries { get; }
        public override bool ExhaustedPages { get; protected set; }
        
        public PoolLeaderboardPage(IPoolLeaderboardSource source, IReadOnlyList<HitbloqPoolLeaderboardEntry> entries, string poolID, int page)
        {
            Source = source;
            Entries = entries;
            this.poolID = poolID;
            this.page = page;
        }
        
        public Task<PoolLeaderboardPage?> Previous(CancellationToken cancellationToken = default) => Source.GetScoresAsync(poolID, cancellationToken, page - 1);

        public async Task<PoolLeaderboardPage?> Next(CancellationToken cancellationToken = default)
        {
            var rankedList = await Source.GetScoresAsync(poolID, cancellationToken, page + 1);
            if (rankedList != null)
            {
                return rankedList;
            }

            ExhaustedPages = true;
            return null;
        }
    }
}