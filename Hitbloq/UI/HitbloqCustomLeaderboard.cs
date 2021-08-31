using Hitbloq.Interfaces;
using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;
using System;
using System.Collections.Generic;

namespace Hitbloq.UI
{
    internal class HitbloqCustomLeaderboard : CustomLeaderboard, IDisposable, ILeaderboardEntriesUpdater
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

        public void LeaderboardEntriesUpdated(List<Entries.LeaderboardEntry> leaderboardEntries)
        {
            if (leaderboardEntries != null && !registered)
            {
                CustomLeaderboardManager.instance?.Register(this);
                registered = true;
            }
            else if (leaderboardEntries == null && registered)
            {
                CustomLeaderboardManager.instance?.Unregister(this);
                registered = false;
            }
        }
    }
}
