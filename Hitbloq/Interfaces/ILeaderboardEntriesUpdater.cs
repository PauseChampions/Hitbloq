using Hitbloq.Entries;
using System.Collections.Generic;

namespace Hitbloq.Interfaces
{
    internal interface ILeaderboardEntriesUpdater
    {
        public void LeaderboardEntriesUpdated(List<HitbloqLeaderboardEntry>? leaderboardEntries);
    }
}
