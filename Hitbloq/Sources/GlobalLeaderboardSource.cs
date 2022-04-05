using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Configuration;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class GlobalLeaderboardSource : ILeaderboardSource
    {
        private readonly IHttpService siraHttpService;
        private Sprite? icon;

        private readonly List<List<HitbloqLeaderboardEntry>> cachedEntries = new();

        public GlobalLeaderboardSource(IHttpService siraHttpService)
        {
            this.siraHttpService = siraHttpService;
        }

        public string HoverHint => "Global";

        public Sprite Icon
        {
            get
            {
                if (icon == null)
                {
                    icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.GlobalIcon.png");
                }
                return icon;
            }
        }

        public bool Scrollable => true;

        public async Task<List<HitbloqLeaderboardEntry>?> GetScoresAsync(IDifficultyBeatmap difficultyBeatmap, CancellationToken cancellationToken = default, int page = 0)
        {
            if (cachedEntries.Count < page + 1)
            {
                try
                {
                    var webResponse = await siraHttpService.GetAsync($"{PluginConfig.Instance.HitbloqURL}api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/scores_extended/{page}", cancellationToken: cancellationToken).ConfigureAwait(false);
                    var scores = await Utils.ParseWebResponse<List<HitbloqLeaderboardEntry>>(webResponse);
                    if (scores != null)
                    {
                        cachedEntries.Add(scores);
                    }
                }
                catch (TaskCanceledException) { }
            }
            return page < cachedEntries.Count ? cachedEntries[page] : null;
        }

        public void ClearCache() => cachedEntries.Clear();
    }
}
