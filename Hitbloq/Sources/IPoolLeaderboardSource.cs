using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Pages;

namespace Hitbloq.Sources
{
    internal interface IPoolLeaderboardSource : ILeaderboardSource
    {
        public Task<PoolLeaderboardPage?> GetScoresAsync(string poolID, CancellationToken cancellationToken = default, int page = 0);
    }
}