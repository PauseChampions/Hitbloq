using SiraUtil;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class GlobalLeaderboardSource : ILeaderboardSource
    {
        private readonly SiraClient siraClient;
        private Sprite _icon;

        public GlobalLeaderboardSource(SiraClient siraClient)
        {
            this.siraClient = siraClient;
        }

        public string HoverHint => "Global";

        public Sprite Icon
        {
            get
            {
                if (_icon == null)
                {
                    _icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.GlobalIcon.png");
                }
                return _icon;
            }
        }

        public async Task<List<Entries.LeaderboardEntry>> GetScoresTask(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null, int page = 0)
        {
            string hash = difficultyBeatmap.level.levelID.Replace(CustomLevelLoader.kCustomLevelPrefixId, "");
            string difficulty = difficultyBeatmap.difficulty.ToString();
            string characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/leaderboard/{hash}%7C_{difficulty}_Solo{characteristic}/scores_extended/{page}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
            if (!webResponse.IsSuccessStatusCode)
            {
                return null;
            }
            return webResponse.ContentToJson<List<Entries.LeaderboardEntry>>();
        }
    }
}
