using Hitbloq.Entries;
using Hitbloq.Interfaces;
using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;
using System;
using System.Collections.Generic;

namespace Hitbloq.UI
{
    internal class HitbloqCustomLeaderboard : CustomLeaderboard, IDisposable, IDifficultyBeatmapUpdater
    {
        private readonly CustomLeaderboardManager customLeaderboardManager;

        private readonly ViewController hitbloqPanelController;
        protected override ViewController panelViewController => hitbloqPanelController;

        private readonly ViewController mainLeaderboardViewController;
        protected override ViewController leaderboardViewController => mainLeaderboardViewController;

        private bool registered = false;

        internal HitbloqCustomLeaderboard(CustomLeaderboardManager customLeaderboardManager, HitbloqPanelController hitbloqPanelController, HitbloqLeaderboardViewController mainLeaderboardViewController)
        {
            this.customLeaderboardManager = customLeaderboardManager;
            this.hitbloqPanelController = hitbloqPanelController;
            this.mainLeaderboardViewController = mainLeaderboardViewController;
        }

        public void Dispose()
        {
            customLeaderboardManager.Unregister(this);
            registered = false;
        }

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo levelInfoEntry)
        {
            if (levelInfoEntry != null && !registered)
            {
                customLeaderboardManager.Register(this);
                registered = true;
            }
            else if (levelInfoEntry == null && registered)
            {
                customLeaderboardManager.Unregister(this);
                registered = false;
            }
        }
    }
}
