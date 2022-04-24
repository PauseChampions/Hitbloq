using Hitbloq.Entries;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal interface IMapLeaderboardSource : ILeaderboardSource
    {
        public Task<List<HitbloqMapLeaderboardEntry>?> GetScoresAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken cancellationToken = default, int page = 0);
        public bool Scrollable { get; }
        public void ClearCache();
    }
}
