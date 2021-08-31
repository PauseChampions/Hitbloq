using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal interface ILeaderboardSource
    {
        public string HoverHint { get; }
        public Sprite Icon { get; }
        public Task<List<Entries.LeaderboardEntry>> GetScoresTask(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null, int page = 0);
    }
}
