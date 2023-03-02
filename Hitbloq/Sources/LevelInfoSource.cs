using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;

namespace Hitbloq.Sources
{
    internal class LevelInfoSource
    {
        private readonly IHttpService siraHttpService;
        private readonly Dictionary<IDifficultyBeatmap, HitbloqLevelInfo?> cache = new();

        public LevelInfoSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
        }

        public async Task<HitbloqLevelInfo?> GetLevelInfoAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken cancellationToken = default)
        {
            if (cache.TryGetValue(difficultyBeatmap, out var cachedValue))
            {
                return cachedValue;
            }
            
            var beatmapString = Utils.DifficultyBeatmapToString(difficultyBeatmap);
            if (beatmapString == null)
            {
                return null;
            }

            try
            {
                var webResponse = await siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/leaderboard/{beatmapString}/info", cancellationToken: cancellationToken).ConfigureAwait(false);
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
