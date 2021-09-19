using Hitbloq.Entries;
using Hitbloq.Utilities;
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
        private readonly UserIDSource userIDSource;
        private Sprite _icon;

        public AroundMeLeaderboardSource(SiraClient siraClient, UserIDSource userIDSource)
        {
            this.siraClient = siraClient;
            this.userIDSource = userIDSource;
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
            HitbloqUserID? userID = await userIDSource.GetUserIDAsync(cancellationToken);
            if (userID == null)
            {
                return null;
            }
            int id = userID.id;

            try
            {
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/nearby_scores/{id}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                return Utils.ParseWebResponse<List<Entries.LeaderboardEntry>>(webResponse);
            }
            catch (TaskCanceledException e) { }
            return null;
        }
    }
}
