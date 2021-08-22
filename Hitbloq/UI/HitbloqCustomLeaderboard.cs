using Hitbloq.UI.ViewControllers;
using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;
using System;
using Zenject;

namespace Hitbloq.UI
{
    internal class HitbloqCustomLeaderboard : CustomLeaderboard, IInitializable, IDisposable
    {
        private readonly ViewController hitbloqPanelController;
        protected override ViewController panelViewController => hitbloqPanelController;

        private readonly ViewController mainLeaderboardViewController;
        protected override ViewController leaderboardViewController => mainLeaderboardViewController;

        internal HitbloqCustomLeaderboard(HitbloqPanelController hitbloqPanelController, MainLeaderboardViewController mainLeaderboardViewController)
        {
            this.hitbloqPanelController = hitbloqPanelController;
            this.mainLeaderboardViewController = mainLeaderboardViewController;
        }

        public void Initialize()
        {
            CustomLeaderboardManager.instance.Register(this);
        }

        public void Dispose()
        {
            CustomLeaderboardManager.instance?.Unregister(this);
        }
    }
}
