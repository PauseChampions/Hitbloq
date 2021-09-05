using Hitbloq.Entries;
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

            string hash = difficultyBeatmap.level.levelID.Replace(CustomLevelLoader.kCustomLevelPrefixId, "");
            string difficulty = difficultyBeatmap.difficulty.ToString();
            string characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            try
            {
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/leaderboard/{hash}%7C_{difficulty}_Solo{characteristic}/info", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                if (!webResponse.IsSuccessStatusCode)
                {
                    cache[difficultyBeatmap] = null;
                    return null;
                }
                HitbloqLevelInfo levelInfo = webResponse.ContentToJson<HitbloqLevelInfo>();

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
