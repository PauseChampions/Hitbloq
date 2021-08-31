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
        private readonly ViewController hitbloqPanelController;
        protected override ViewController panelViewController => hitbloqPanelController;

        private readonly ViewController mainLeaderboardViewController;
        protected override ViewController leaderboardViewController => mainLeaderboardViewController;

        private bool registered = false;

        internal HitbloqCustomLeaderboard(HitbloqPanelController hitbloqPanelController, HitbloqLeaderboardViewController mainLeaderboardViewController)
        {
            this.hitbloqPanelController = hitbloqPanelController;
            this.mainLeaderboardViewController = mainLeaderboardViewController;
        }

        public void Dispose()
        {
            CustomLeaderboardManager.instance?.Unregister(this);
            registered = false;
        }

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, LevelInfoEntry levelInfoEntry)
        {
            if (levelInfoEntry != null && !registered)
            {
                CustomLeaderboardManager.instance?.Register(this);
                registered = true;
            }
            else if (levelInfoEntry == null && registered)
            {
                CustomLeaderboardManager.instance?.Unregister(this);
                registered = false;
            }
        }
    }
}
