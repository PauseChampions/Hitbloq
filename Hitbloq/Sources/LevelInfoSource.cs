using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class LevelInfoSource
    {
        private readonly SiraClient siraClient;
        private readonly Dictionary<IDifficultyBeatmap, HitbloqLevelInfo> cache;

        public LevelInfoSource(SiraClient siraClient)
        {
            this.siraClient = siraClient;
            cache = new Dictionary<IDifficultyBeatmap, HitbloqLevelInfo>();
        }

        public async Task<HitbloqLevelInfo> GetLevelInfoAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null)
        {
            if (cache.TryGetValue(difficultyBeatmap, out HitbloqLevelInfo cachedValue))
            {
                return cachedValue;
            }

            try
            {
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/info", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                HitbloqLevelInfo levelInfo = Utils.ParseWebResponse<HitbloqLevelInfo>(webResponse);

                cache[difficultyBeatmap] = levelInfo;
                return levelInfo;
            }
            catch (TaskCanceledException e)
            {
                return null;
            }
        }
    }
}
