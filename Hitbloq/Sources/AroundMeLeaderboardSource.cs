using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class AroundMeLeaderboardSource : ILeaderboardSource
    {
        private readonly IHttpService siraHttpService;
        private readonly UserIDSource userIDSource;
        private Sprite _icon;

        private List<HitbloqLeaderboardEntry> cachedEntries;

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
                if (_icon == null)
                {
                    _icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.PlayerIcon.png");
                }
                return _icon;
            }
        }

        public bool Scrollable => false;

        public async Task<List<HitbloqLeaderboardEntry>> GetScoresTask(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null, int page = 0)
        {
            if (cachedEntries == null)
            {
                HitbloqUserID userID = await userIDSource.GetUserIDAsync(cancellationToken);
                if (userID.ID == -1)
                {
                    return null;
                }
                int id = userID.ID;

                try
                {
                    IHttpResponse webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/nearby_scores/{id}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    cachedEntries = await Utils.ParseWebResponse<List<HitbloqLeaderboardEntry>>(webResponse);
                }
                catch (TaskCanceledException) { }
            }
            return cachedEntries;
        }

        public void ClearCache() => cachedEntries = null;
    }
}
