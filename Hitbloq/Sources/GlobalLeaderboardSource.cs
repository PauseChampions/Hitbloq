using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class GlobalLeaderboardSource : ILeaderboardSource
    {
        private readonly IHttpService siraHttpService;
        private Sprite _icon;

        private List<List<HitbloqLeaderboardEntry>> cachedEntries;

        public GlobalLeaderboardSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
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

        public bool Scrollable => true;

        public async Task<List<HitbloqLeaderboardEntry>> GetScoresTask(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null, int page = 0)
        {
            if (cachedEntries.Count < page + 1)
            {
                try
                {
                    var webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/scores_extended/{page}", cancellationToken: cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    cachedEntries.Add(await Utils.ParseWebResponse<List<HitbloqLeaderboardEntry>>(webResponse));
                }
                catch (TaskCanceledException) { }
            }
            return page < cachedEntries.Count ? cachedEntries[page] : null;
        }

        public void ClearCache() => cachedEntries = new List<List<HitbloqLeaderboardEntry>>();
    }
}
