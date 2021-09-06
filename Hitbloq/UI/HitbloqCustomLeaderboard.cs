using Hitbloq.Entries;
using Hitbloq.Interfaces;
using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;
using System;

namespace Hitbloq.UI
{
    internal class HitbloqCustomLeaderboard : CustomLeaderboard, IDisposable, IDifficultyBeatmapUpdater
    {
        private readonly CustomLeaderboardManager customLeaderboardManager;

        private readonly ViewController hitbloqPanelController;
        protected override ViewController panelViewController => hitbloqPanelController;

        private readonly HitbloqLeaderboardViewController mainLeaderboardViewController;
        protected override ViewController leaderboardViewController => mainLeaderboardViewController;

        internal HitbloqCustomLeaderboard(CustomLeaderboardManager customLeaderboardManager, HitbloqPanelController hitbloqPanelController, HitbloqLeaderboardViewController mainLeaderboardViewController)
        {
            this.customLeaderboardManager = customLeaderboardManager;
            this.hitbloqPanelController = hitbloqPanelController;
            this.mainLeaderboardViewController = mainLeaderboardViewController;
        }

        public void Dispose()
        {
            customLeaderboardManager.Unregister(this);
        }

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo levelInfoEntry)
        {
            if (levelInfoEntry != null)
            {
                customLeaderboardManager.Register(this);
            }
            else if (levelInfoEntry == null)
            {
                customLeaderboardManager.Unregister(this);
            }
        }


    }
}
