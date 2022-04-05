using Hitbloq.Entries;
using Hitbloq.Sources;
using Hitbloq.UI;
using SiraUtil.Web;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Hitbloq.Other
{
    internal class LeaderboardRefresher
    {
        private readonly IHttpService siraHttpService;
        private readonly BeatmapListener beatmapListener;
        private readonly HitbloqPanelController hitbloqPanelController;
        private readonly UserIDSource userIDSource;
        private readonly LevelInfoSource levelInfoSource;

        public LeaderboardRefresher(IHttpService siraHttpService, BeatmapListener beatmapListener,
            HitbloqPanelController hitbloqPanelController, UserIDSource userIDSource, LevelInfoSource levelInfoSource)
        {
            this.siraHttpService = siraHttpService;
            this.beatmapListener = beatmapListener;
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
                IHttpResponse webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/update_user/{userID.ID}").ConfigureAwait(false);
                HitbloqRefreshEntry refreshEntry = await Utilities.Utils.ParseWebResponse<HitbloqRefreshEntry>(webResponse);

                if (refreshEntry != null && refreshEntry.Error == null)
                {
                    // Try checking action queue if our action is completed, timeout at 7 times
                    for (int i = 0; i < 7; i++)
                    {
                        await Task.Delay(3000);

                        webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/actions").ConfigureAwait(false);
                        List<HitbloqActionQueueEntry> actionQueueEntries = await Utilities.Utils.ParseWebResponse<List<HitbloqActionQueueEntry>>(webResponse);

                        if (actionQueueEntries == null || !actionQueueEntries.Exists(entry => entry.ID == refreshEntry.ID))
                        {
                            hitbloqPanelController.LoadingActive = false;
                            hitbloqPanelController.PromptText = "<color=green>Score refreshed!</color>";
                            return true;
                        }
                    }
                    hitbloqPanelController.PromptText = "<color=red>The action queue is very busy, your score cannot be refreshed for now.</color>";
                }
                else if (refreshEntry != null && refreshEntry.Error != null)
                {
                    hitbloqPanelController.PromptText = $"<color=red>Error: {refreshEntry.Error}</color>";
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
            if (!userID.Registered)
            {
                return false;
            }

            if (beatmapListener.lastPlayedDifficultyBeatmap != null)
            {
                HitbloqLevelInfo levelInfo = await levelInfoSource.GetLevelInfoAsync(beatmapListener.lastPlayedDifficultyBeatmap);
                if (levelInfo != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
