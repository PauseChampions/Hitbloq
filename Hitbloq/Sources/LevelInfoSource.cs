using Hitbloq.Entries;
using SiraUtil;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class LevelInfoSource
    {
        private readonly SiraClient siraClient;

        public LevelInfoSource(SiraClient siraClient)
        {
            this.siraClient = siraClient;
        }

        public async Task<LevelInfoEntry> GetLevelInfoAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken)
        {
            string hash = difficultyBeatmap.level.levelID.Replace(CustomLevelLoader.kCustomLevelPrefixId, "");
            string difficulty = difficultyBeatmap.difficulty.ToString();
            string characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/leaderboard/{hash}%7C_{difficulty}_Solo{characteristic}/info", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
            if (!webResponse.IsSuccessStatusCode)
            {
                return null;
            }
            return webResponse.ContentToJson<LevelInfoEntry>();
        }
    }
}
