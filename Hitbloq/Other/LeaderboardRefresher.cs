using Hitbloq.Entries;
using Hitbloq.Sources;
using Hitbloq.UI;
using IPA.Utilities;
using SiraUtil;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Other
{
    internal class LeaderboardRefresher
    {
        private readonly SiraClient siraClient;
        private readonly ResultsViewController resultsViewController;
        private readonly StandardLevelDetailViewController standardLevelDetailViewController;
        private readonly HitbloqPanelController hitbloqPanelController;
        private readonly UserIDSource userIDSource;
        private readonly LevelInfoSource levelInfoSource;

        public LeaderboardRefresher(SiraClient siraClient, ResultsViewController resultsViewController, StandardLevelDetailViewController standardLevelDetailViewController,
            HitbloqPanelController hitbloqPanelController, UserIDSource userIDSource, LevelInfoSource levelInfoSource)
        {
            this.siraClient = siraClient;
            this.resultsViewController = resultsViewController;
            this.standardLevelDetailViewController = standardLevelDetailViewController;
            this.hitbloqPanelController = hitbloqPanelController;
            this.userIDSource = userIDSource;
            this.levelInfoSource = levelInfoSource;
        }

        public async Task<bool> Refresh()
        {
            if (await RefreshNeeded())
            {
                hitbloqPanelController.LoadingActive = true;
                hitbloqPanelController.PromptText = "Refreshing Score...";

                await Task.Delay(3000);
                HitbloqUserID userID = await userIDSource.GetUserIDAsync();
                WebResponse webResponse = await siraClient.GetAsync($"https://hitbloq.com/api/update_user/{userID.id}", CancellationToken.None).ConfigureAwait(false);
                HitbloqRefreshEntry refreshEntry = Utilities.Utils.ParseWebResponse<HitbloqRefreshEntry>(webResponse);

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
                            hitbloqPanelController.LoadingActive = false;
                            hitbloqPanelController.PromptText = "<color=green>Score refreshed!</color>";
                            return true;
                        }
                    }
                    hitbloqPanelController.PromptText = "<color=red>The action queue is very busy, your score cannot be refreshed for now.</color>";
                }
                else if (refreshEntry.error != null)
                {
                    hitbloqPanelController.PromptText = $"<color=red>Error: {refreshEntry.error}</color>";
                }
                else
                {
                    hitbloqPanelController.PromptText = "<color=red>Hitbloq servers are not responding, please try again later.</color>";
                }
            }
            hitbloqPanelController.LoadingActive = false;
            return false;
        }

        private async Task<bool> RefreshNeeded()
        {
            HitbloqUserID userID = await userIDSource.GetUserIDAsync();
            if (!userID.registered)
            {
                return false;
            }

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
