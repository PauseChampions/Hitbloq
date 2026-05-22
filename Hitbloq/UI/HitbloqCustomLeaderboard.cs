using System;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.UI.ViewControllers;
using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;

namespace Hitbloq.UI
{
	internal class HitbloqCustomLeaderboard : CustomLeaderboard, IDisposable, IBeatmapKeyUpdater
	{
		private readonly CustomLeaderboardManager _customLeaderboardManager;

		private readonly HitbloqLeaderboardViewController _hitbloqLeaderboardViewController;
		private bool _registered;

		internal HitbloqCustomLeaderboard(CustomLeaderboardManager customLeaderboardManager, HitbloqPanelController hitbloqPanelController, HitbloqLeaderboardViewController mainLeaderboardViewController)
		{
			_customLeaderboardManager = customLeaderboardManager;
			panelViewController = hitbloqPanelController;
			_hitbloqLeaderboardViewController = mainLeaderboardViewController;
		}

		protected override ViewController panelViewController { get; }
		protected override ViewController leaderboardViewController => _hitbloqLeaderboardViewController;

		public void BeatmapKeyUpdated(BeatmapKey beatmapKey, HitbloqLevelInfo? levelInfoEntry)
		{
			if (_registered)
			{
				return;
			}

			_customLeaderboardManager.Register(this);
			_registered = true;
		}

		public void Dispose()
		{
			_customLeaderboardManager.Unregister(this);
		}
	}
}
