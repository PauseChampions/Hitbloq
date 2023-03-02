using System.Collections.Generic;
using Hitbloq.Entries;

namespace Hitbloq.Interfaces
{
    internal interface ILeaderboardEntriesUpdater
    {
        public void LeaderboardEntriesUpdated(List<HitbloqMapLeaderboardEntry>? leaderboardEntries);
    }
}
