using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class AroundMeLeaderboardSource : IMapLeaderboardSource
    {
        private readonly IHttpService siraHttpService;
        private readonly UserIDSource userIDSource;
        private Sprite? icon;

        private List<HitbloqMapLeaderboardEntry>? cachedEntries;

        public AroundMeLeaderboardSource(IHttpService siraHttpService, UserIDSource userIDSource)
        {
            this.siraHttpService = siraHttpService;
            this.userIDSource = userIDSource;
        }

        public string HoverHint => "Around Me";

        public Sprite Icon
        {
            get
            {
                if (icon == null)
                {
                    icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.PlayerIcon.png");
                }
                return icon;
            }
        }

        public bool Scrollable => false;

        public async Task<List<HitbloqMapLeaderboardEntry>?> GetScoresAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken cancellationToken = default, int page = 0)
        {
            if (cachedEntries == null)
            {
                var beatmapString = Utils.DifficultyBeatmapToString(difficultyBeatmap);
                if (beatmapString == null)
                {
                    return null;
                }
                
                var userID = await userIDSource.GetUserIDAsync(cancellationToken);
                if (userID == null || userID.ID == -1)
                {
                    return null;
                }
                var id = userID.ID;

                try
                {
                    var webResponse = await siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}/api/leaderboard/{beatmapString}/nearby_scores_extended/{id}", cancellationToken: cancellationToken).ConfigureAwait(false);
                    cachedEntries = await Utils.ParseWebResponse<List<HitbloqMapLeaderboardEntry>>(webResponse);
                }
                catch (TaskCanceledException) { }
            }
            return cachedEntries;
        }

        public void ClearCache() => cachedEntries = null;
    }
}
