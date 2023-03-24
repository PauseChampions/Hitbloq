using System;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.UI.ViewControllers;
using HMUI;
using LeaderboardCore.Managers;
using LeaderboardCore.Models;

namespace Hitbloq.UI
{
	internal class HitbloqCustomLeaderboard : CustomLeaderboard, IDisposable, IDifficultyBeatmapUpdater
	{
		private readonly CustomLeaderboardManager _customLeaderboardManager;

		private readonly HitbloqLeaderboardViewController _hitbloqLeaderboardViewController;

		internal HitbloqCustomLeaderboard(CustomLeaderboardManager customLeaderboardManager, HitbloqPanelController hitbloqPanelController, HitbloqLeaderboardViewController mainLeaderboardViewController)
		{
			_customLeaderboardManager = customLeaderboardManager;
			panelViewController = hitbloqPanelController;
			_hitbloqLeaderboardViewController = mainLeaderboardViewController;
		}

		protected override ViewController panelViewController { get; }
		protected override ViewController leaderboardViewController => _hitbloqLeaderboardViewController;

		public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo? levelInfoEntry)
		{
			if (levelInfoEntry != null)
			{
				_customLeaderboardManager.Register(this);
			}
			else if (levelInfoEntry == null)
			{
				_customLeaderboardManager.Unregister(this);
			}
		}

		public void Dispose()
		{
			_customLeaderboardManager.Unregister(this);
		}
	}
}