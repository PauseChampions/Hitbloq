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
                
                var userID = await userIDSource.GetUserIDAsync();

                if (userID == null)
                {
                    return false;
                }
                
                var webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/update_user/{userID.ID}").ConfigureAwait(false);
                var refreshEntry = await Utilities.Utils.ParseWebResponse<HitbloqRefreshEntry>(webResponse);

                if (refreshEntry is {Error: null})
                {
                    // Try checking action queue if our action is completed, timeout at 7 times
                    for (var i = 0; i < 7; i++)
                    {
                        await Task.Delay(3000);

                        webResponse = await siraHttpService.GetAsync($"https://hitbloq.com/api/actions").ConfigureAwait(false);
                        var actionQueueEntries = await Utilities.Utils.ParseWebResponse<List<HitbloqActionQueueEntry>>(webResponse);

                        if (actionQueueEntries == null || !actionQueueEntries.Exists(entry => entry.ID == refreshEntry.ID))
                        {
                            hitbloqPanelController.LoadingActive = false;
                            hitbloqPanelController.PromptText = "<color=green>Score refreshed!</color>";
                            return true;
                        }
                    }
                    hitbloqPanelController.PromptText = "<color=red>The action queue is very busy, your score cannot be refreshed for now.</color>";
                }
                else if (refreshEntry is {Error: { }})
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
            var userID = await userIDSource.GetUserIDAsync();
            if (userID == null || !userID.Registered)
            {
                return false;
            }

            if (beatmapListener.LastPlayedDifficultyBeatmap != null)
            {
                var levelInfo = await levelInfoSource.GetLevelInfoAsync(beatmapListener.LastPlayedDifficultyBeatmap);
                if (levelInfo != null)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
