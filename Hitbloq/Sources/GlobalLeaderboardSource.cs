using Hitbloq.Entries;
using Hitbloq.Utilities;
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

        private List<List<HitbloqLeaderboardEntry>> cachedEntries;

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

        public bool Scrollable => true;

        public async Task<List<HitbloqLeaderboardEntry>> GetScoresTask(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null, int page = 0)
        {
            if (cachedEntries.Count < page + 1)
            {
                try
                {
                    WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/scores_extended/{page}", cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    cachedEntries.Add(Utils.ParseWebResponse<List<HitbloqLeaderboardEntry>>(webResponse));
                }
                catch (TaskCanceledException) { }
            }
            return page < cachedEntries.Count ? cachedEntries[page] : null;
        }

        public void ClearCache() => cachedEntries = new List<List<HitbloqLeaderboardEntry>>();
    }
}
