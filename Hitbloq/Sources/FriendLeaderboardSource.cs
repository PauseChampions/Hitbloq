using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil.Web;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class FriendsLeaderboardSource : ILeaderboardSource
    {
        private readonly IHttpService siraHttpService;
        private Sprite _icon;

        private readonly UserIDSource userIDSource;
        private readonly FriendIDSource friendIDSource;

        private List<List<HitbloqLeaderboardEntry>> cachedEntries;

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
                if (_icon == null)
                {
                    _icon = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.FriendsIcon.png");
                }
                return _icon;
            }
        }

        public bool Scrollable => true;

        public async Task<List<HitbloqLeaderboardEntry>> GetScoresTask(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null, int page = 0)
        {
            if (cachedEntries == null)
            {
                HitbloqUserID userID = await userIDSource.GetUserIDAsync(cancellationToken);
                List<int> friendIDs = await friendIDSource.GetFriendIDsAsync(cancellationToken);

                if (userID.ID == -1 || friendIDs == null)
                {
                    return null;
                }

                int friendCount = friendIDs == null ? 0 : friendIDs.Count;
                int[] ids = new int[friendCount + 1];
                ids[0] = userID.ID;
                for (int i = 0; i < friendCount; i++)
                {
                    ids[i + 1] = friendIDs[i];
                }

                try
                {
                    var content = new Dictionary<string, int[]>
                    {
                        { "friends", ids}
                    };
                    IHttpResponse webResponse = await siraHttpService.PostAsync($"https://hitbloq.com/api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/friends", content, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                    // like an hour of debugging and we had to remove the slash from the end of the url. that was it. not pog.

                    List<HitbloqLeaderboardEntry> leaderboardEntries = await Utils.ParseWebResponse<List<HitbloqLeaderboardEntry>>(webResponse);
                    cachedEntries = new List<List<HitbloqLeaderboardEntry>>();

                    if (leaderboardEntries != null)
                    {
                        // Splitting entries into lists of 10
                        int p = 0;
                        cachedEntries.Add(new List<HitbloqLeaderboardEntry>());
                        for (int i = 0; i < leaderboardEntries.Count; i++)
                        {
                            if (cachedEntries[p].Count == 10)
                            {
                                cachedEntries.Add(new List<HitbloqLeaderboardEntry>());
                                p++;
                            }
                            cachedEntries[p].Add(leaderboardEntries[i]);
                        }
                    }
                }
                catch (TaskCanceledException) { }
            }

            if (cachedEntries == null)
            {
                return null;
            }

            return page < cachedEntries.Count ? cachedEntries[page] : null;
        }

        public void ClearCache() => cachedEntries = null;
    }
}
