using Hitbloq.Entries;
using Hitbloq.Utilities;
using SiraUtil;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace Hitbloq.Sources
{
    internal class FriendsLeaderboardSource : ILeaderboardSource
    {
        private readonly SiraClient siraClient;
        private Sprite _icon;

        private readonly UserIDSource userIDSource;
        private readonly FriendIDSource friendIDSource;

        public FriendsLeaderboardSource(SiraClient siraClient, UserIDSource userIDSource, FriendIDSource friendIDSource)
        {
            this.siraClient = siraClient;
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

        public async Task<List<Entries.LeaderboardEntry>> GetScoresTask(IDifficultyBeatmap difficultyBeatmap, CancellationToken? cancellationToken = null, int page = 0)
        {
            HitbloqUserID userID = await userIDSource.GetUserIDAsync(cancellationToken);
            List<HitbloqFriendID> friendIDs = await friendIDSource.GetLevelInfoAsync(cancellationToken);
            if (friendIDs == null)
            {
                return null;
            }

            int[] ids = new int[friendIDs.Count + 1];

            ids[0] = userID.id;

            for (int i = 0; i < friendIDs.Count; i++)
            {
                ids[i + 1] = friendIDs[i].id;
            }
            

            try
            {
                var content = new Dictionary<string, int[]>
                {
                    { "friends", ids}
                };
                WebResponse webResponse = await siraClient.PostAsync($"https://hitbloq.com/api/leaderboard/{Utils.DifficultyBeatmapToString(difficultyBeatmap)}/friends", content, cancellationToken ?? CancellationToken.None).ConfigureAwait(false);
                // like an hour of debugging and we had to remove the slash from the end of the url. that was it. not pog.

                return Utils.ParseWebResponse<List<Entries.LeaderboardEntry>>(webResponse);
            }
            catch (TaskCanceledException) { }
            return null;
        }
    }
}
