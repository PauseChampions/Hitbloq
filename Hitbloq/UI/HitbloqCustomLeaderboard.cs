using System;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;

namespace Hitbloq.UI
{
    internal class HitbloqCustomLeaderboard : CustomLeaderboard, IDisposable, IDifficultyBeatmapUpdater
    {
        private readonly CustomLeaderboardManager customLeaderboardManager;

        protected override ViewController panelViewController { get; }

        private readonly HitbloqLeaderboardViewController hitbloqLeaderboardViewController;
        protected override ViewController leaderboardViewController => hitbloqLeaderboardViewController;

        internal HitbloqCustomLeaderboard(CustomLeaderboardManager customLeaderboardManager, HitbloqPanelController hitbloqPanelController, HitbloqLeaderboardViewController mainLeaderboardViewController)
        {
            this.customLeaderboardManager = customLeaderboardManager;
            this.panelViewController = hitbloqPanelController;
            hitbloqLeaderboardViewController = mainLeaderboardViewController;
        }

        public void Dispose()
        {
            customLeaderboardManager.Unregister(this);
        }

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo? levelInfoEntry)
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
