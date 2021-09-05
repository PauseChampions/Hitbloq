using Hitbloq.Entries;
using IPA.Utilities;
using SiraUtil;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Sources
{
    internal class LeaderboardRefresher
    {
        private readonly SiraClient siraClient;
        private readonly ResultsViewController resultsViewController;
        private readonly StandardLevelDetailViewController standardLevelDetailViewController;
        private readonly UserInfoSource userInfoSource;
        private readonly LevelInfoSource levelInfoSource;

        public LeaderboardRefresher(SiraClient siraClient, ResultsViewController resultsViewController, StandardLevelDetailViewController standardLevelDetailViewController,
            UserInfoSource userInfoSource, LevelInfoSource levelInfoSource)
        {
            this.siraClient = siraClient;
            this.resultsViewController = resultsViewController;
            this.standardLevelDetailViewController = standardLevelDetailViewController;
            this.userInfoSource = userInfoSource;
            this.levelInfoSource = levelInfoSource;
        }

        public async Task<bool> Refresh()
        {
            if (await RefreshNeeded())
            {
                Plugin.Log.Debug("Refreshing");
                await Task.Delay(3000);
                HitbloqUserInfo userInfo = await userInfoSource.GetUserInfoAsync();
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/update_user/{userInfo.id}", CancellationToken.None).ConfigureAwait(false);
                HitbloqRefreshEntry refreshEntry = Utilities.Utils.ParseWebResponse<HitbloqRefreshEntry>(webResponse);
                Plugin.Log.Debug("Refreshed");

                if (refreshEntry != null && refreshEntry.error == null)
                {
                    // Try checking action queue if our action is completed, timeout at 7 times
                    for (int i = 0; i < 7; i++)
                    {
                        await Task.Delay(3000);

                        webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/actions", CancellationToken.None).ConfigureAwait(false);
                        List<HitbloqActionQueueEntry> actionQueueEntries = Utilities.Utils.ParseWebResponse<List<HitbloqActionQueueEntry>>(webResponse);

                        if (actionQueueEntries == null || !actionQueueEntries.Exists(entry => entry.id == refreshEntry.id))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        private async Task<bool> RefreshNeeded()
        {
            Plugin.Log.Debug("Refresh Needed?");
            IDifficultyBeatmap lastPlayedBeatmap = resultsViewController.GetField<IDifficultyBeatmap, ResultsViewController>("_difficultyBeatmap");
            HitbloqLevelInfo levelInfo;

            if (lastPlayedBeatmap != null)
            {
                levelInfo = await levelInfoSource.GetLevelInfoAsync(lastPlayedBeatmap);
                if (levelInfo != null)
                {
                    return true;
                }
            }

            levelInfo = await levelInfoSource.GetLevelInfoAsync(standardLevelDetailViewController.selectedDifficultyBeatmap);
            if (levelInfo != null)
            {
                return true;
            }

            return false;
        }
    }
}
