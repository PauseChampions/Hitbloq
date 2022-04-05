using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class LevelInfoSource
    {
        private readonly IHttpService siraHttpService;
        private readonly Dictionary<IDifficultyBeatmap, HitbloqLevelInfo> cache;

        public LevelInfoSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
            cache = new Dictionary<IDifficultyBeatmap, HitbloqLevelInfo>();
        }

        public async Task<HitbloqLevelInfo> GetLevelInfoAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null)
        {
            if (cache.TryGetValue(difficultyBeatmap, out var cachedValue))
            {
                return cachedValue;
            }

            try
            {
                var webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/info", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                var levelInfo = await Utils.ParseWebResponse<HitbloqLevelInfo>(webResponse);

                cache[difficultyBeatmap] = levelInfo;
                return levelInfo;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }
    }
}
