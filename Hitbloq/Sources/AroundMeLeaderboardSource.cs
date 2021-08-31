using SiraUtil;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class AroundMeLeaderboardSource : ILeaderboardSource
    {
        private readonly SiraClient siraClient;
        private Sprite _icon;
        private int id;

        public AroundMeLeaderboardSource(SiraClient siraClient)
        {
            this.siraClient = siraClient;
            this.id = 143; // todo
            
        }

        public string HoverHint => "Around Me";

        public Sprite Icon
        {
            get
            {
                if (_icon == null)
                {
                    _icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.PlayerIcon.png");
                }
                return _icon;
            }
        }

        public bool Scrollable => false;

        public async Task<List<Entries.LeaderboardEntry>> GetScoresTask(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null, int page = 0)
        {
            string hash = difficultyBeatmap.level.levelID.Replace(CustomLevelLoader.kCustomLevelPrefixId, "");
            string difficulty = difficultyBeatmap.difficulty.ToString();
            string characteristic = difficultyBeatmap.parentDifficultyBeatmapSet.beatmapCharacteristic.serializedName;

            WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/leaderboard/{hash}%7C_{difficulty}_Solo{characteristic}/nearby_scores/{id}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
            Plugin.Log.Debug($"{webResponse.ContentToBytes().Length} PixelBoom :D");
            if (!webResponse.IsSuccessStatusCode || webResponse.ContentToBytes().Length == 3) // 403 where?
            {
                return null;
            }
            return webResponse.ContentToJson<List<Entries.LeaderboardEntry>>();
        }
    }
}
