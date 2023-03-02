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
    internal class FriendsLeaderboardSource : IMapLeaderboardSource
    {
        private readonly IHttpService siraHttpService;
        private Sprite? icon;

        private readonly UserIDSource userIDSource;
        private readonly FriendIDSource friendIDSource;

        private List<List<HitbloqMapLeaderboardEntry>>? cachedEntries;

        public FriendsLeaderboardSource(IHttpService siraHttpService, UserIDSource userIDSource, FriendIDSource friendIDSource)
        {
            this.siraHttpService = siraHttpService;
            this.userIDSource = userIDSource;
            this.friendIDSource = friendIDSource;
        }

        public string HoverHint => "Friends";

        public Sprite Icon
        {
            get
            {
                if (icon == null)
                {
                    icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.FriendsIcon.png");
                }
                return icon;
            }
        }

        public bool Scrollable => true;

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
                var friendIDs = await friendIDSource.GetFriendIDsAsync(cancellationToken);

                if (userID == null || userID.ID == -1)
                {
                    return null;
                }

                friendIDs.Add(userID.ID);

                try
                {
                    var content = new Dictionary<string, int[]>
                    {
                        { "friends", friendIDs.ToArray()}
                    };
                    var webResponse = await siraHttpService.PostAsync($"{PluginConfig.Instance.HitbloqURL}/api/leaderboard/{beatmapString}/friends_extended", content, cancellationToken).ConfigureAwait(false);
                    // like an hour of debugging and we had to remove the slash from the end of the url. that was it. not pog.

                    var leaderboardEntries = await Utils.ParseWebResponse<List<HitbloqMapLeaderboardEntry>>(webResponse);
                    cachedEntries = new List<List<HitbloqMapLeaderboardEntry>>();

                    if (leaderboardEntries != null)
                    {
                        // Splitting entries into lists of 10
                        var p = 0;
                        cachedEntries.Add(new List<HitbloqMapLeaderboardEntry>());
                        for (var i = 0; i < leaderboardEntries.Count; i++)
                        {
                            if (cachedEntries[p].Count == 10)
                            {
                                cachedEntries.Add(new List<HitbloqMapLeaderboardEntry>());
                                p++;
                            }
                            cachedEntries[p].Add(leaderboardEntries[i]);
                        }
                    }
                }
                catch (TaskCanceledException) { }
            }

            return page < cachedEntries?.Count ? cachedEntries[page] : null;
        }

        public void ClearCache() => cachedEntries = null;
    }
}
