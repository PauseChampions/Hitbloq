using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Sources;
using Hitbloq.UI;
using Hitbloq.Utilities;
using IPA.Utilities.Async;
using LeaderboardCore.Interfaces;
using UnityEngine.SceneManagement;
using Zenject;

namespace Hitbloq.Managers
{
	internal class HitbloqManager : IInitializable, IDisposable, INotifyLeaderboardSet
	{
		private readonly List<IDifficultyBeatmapUpdater> _difficultyBeatmapUpdaters;
		private readonly HitbloqEventModalViewController _hitbloqEventModalViewController;
		private readonly HitbloqFlowCoordinator _hitbloqFlowCoordinator;
		private readonly HitbloqLeaderboardViewController _hitbloqLeaderboardViewController;
		private readonly HitbloqPanelController _hitbloqPanelController;
		private readonly HitbloqProfileModalController _hitbloqProfileModalController;
		private readonly List<ILeaderboardEntriesUpdater> _leaderboardEntriesUpdaters;
		private readonly LeaderboardRefresher _leaderboardRefresher;
		private readonly LevelInfoSource _levelInfoSource;

		private readonly List<INotifyUserRegistered> _notifyUserRegistereds;
		private readonly List<INotifyViewActivated> _notifyViewActivateds;
		private readonly List<IPoolUpdater> _poolUpdaters;

		private readonly UserIDSource _userIDSource;

		private string? _currentPool;
		private CancellationTokenSource? _leaderboardTokenSource;
		private CancellationTokenSource? _levelInfoTokenSource;
		private bool _scoreAlreadyUploaded;

		private IDifficultyBeatmap? _selectedDifficultyBeatmap;

		public HitbloqManager(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, HitbloqPanelController hitbloqPanelController, HitbloqProfileModalController hitbloqProfileModalController, HitbloqEventModalViewController hitbloqEventModalViewController, HitbloqFlowCoordinator hitbloqFlowCoordinator, UserIDSource userIDSource, LevelInfoSource levelInfoSource, LeaderboardRefresher leaderboardRefresher, List<INotifyUserRegistered> notifyUserRegistereds, List<IDifficultyBeatmapUpdater> difficultyBeatmapUpdaters, List<INotifyViewActivated> notifyViewActivateds, List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters, List<IPoolUpdater> poolUpdaters)
		{
			_hitbloqLeaderboardViewController = hitbloqLeaderboardViewController;
			_hitbloqPanelController = hitbloqPanelController;
			_hitbloqProfileModalController = hitbloqProfileModalController;
			_hitbloqEventModalViewController = hitbloqEventModalViewController;
			_hitbloqFlowCoordinator = hitbloqFlowCoordinator;

			_userIDSource = userIDSource;
			_levelInfoSource = levelInfoSource;
			_leaderboardRefresher = leaderboardRefresher;

			_notifyUserRegistereds = notifyUserRegistereds;
			_difficultyBeatmapUpdaters = difficultyBeatmapUpdaters;
			_notifyViewActivateds = notifyViewActivateds;
			_leaderboardEntriesUpdaters = leaderboardEntriesUpdaters;
			_poolUpdaters = poolUpdaters;
		}

		public void Dispose()
		{
			_userIDSource.UserRegisteredEvent -= OnUserRegistered;

			_hitbloqLeaderboardViewController.didActivateEvent -= OnViewActivated;
			_hitbloqLeaderboardViewController.PageRequested -= OnPageRequested;

			_hitbloqPanelController.PoolChangedEvent -= OnPoolChanged;
			_hitbloqPanelController.RankTextClickedEvent -= OnRankTextClicked;
			_hitbloqPanelController.LogoClickedEvent -= OnLogoClicked;
			_hitbloqPanelController.EventClickedEvent -= OnEventClicked;

			SceneManager.activeSceneChanged -= SceneManagerOnactiveSceneChanged;
		}

		public void Initialize()
		{
			_userIDSource.UserRegisteredEvent += OnUserRegistered;

			_hitbloqLeaderboardViewController.didActivateEvent += OnViewActivated;
			_hitbloqLeaderboardViewController.PageRequested += OnPageRequested;

			_hitbloqPanelController.PoolChangedEvent += OnPoolChanged;
			_hitbloqPanelController.RankTextClickedEvent += OnRankTextClicked;
			_hitbloqPanelController.LogoClickedEvent += OnLogoClicked;
			_hitbloqPanelController.EventClickedEvent += OnEventClicked;

			SceneManager.activeSceneChanged += SceneManagerOnactiveSceneChanged;
		}

		public void OnLeaderboardSet(IDifficultyBeatmap? difficultyBeatmap)
		{
			_ = OnLeaderboardSetAsync(difficultyBeatmap);
		}

		public void OnScoreUploaded()
		{
			_ = OnScoreUploadAsync();
		}

		private async Task OnScoreUploadAsync()
		{
			if (!_scoreAlreadyUploaded && await _leaderboardRefresher.Refresh())
			{
				_scoreAlreadyUploaded = true;
				await OnLeaderboardSetAsync(_selectedDifficultyBeatmap);
			}
		}

		private async Task OnLeaderboardSetAsync(IDifficultyBeatmap? difficultyBeatmap)
		{
			if (difficultyBeatmap != null)
			{
				_selectedDifficultyBeatmap = difficultyBeatmap;
				_levelInfoTokenSource?.Cancel();
				_levelInfoTokenSource?.Dispose();
				HitbloqLevelInfo? levelInfoEntry = null;

				if (difficultyBeatmap.level is CustomPreviewBeatmapLevel)
				{
					_levelInfoTokenSource = new CancellationTokenSource();
					levelInfoEntry = await _levelInfoSource.GetLevelInfoAsync(difficultyBeatmap, _levelInfoTokenSource.Token);
				}

				if (levelInfoEntry != null)
				{
					if (levelInfoEntry.Pools.Count == 0)
					{
						levelInfoEntry = null;
					}
				}

				foreach (var difficultyBeatmapUpdater in _difficultyBeatmapUpdaters)
				{
					await UnityMainThreadTaskScheduler.Factory.StartNew(() => difficultyBeatmapUpdater.DifficultyBeatmapUpdated(difficultyBeatmap, levelInfoEntry));
				}
			}
		}

		private void OnUserRegistered()
		{
			foreach (var notifyUserRegistered in _notifyUserRegistereds)
			{
				notifyUserRegistered.UserRegistered();
			}
		}

		private void OnViewActivated(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			foreach (var notifyViewActivated in _notifyViewActivateds)
			{
				notifyViewActivated.ViewActivated(_hitbloqLeaderboardViewController, firstActivation, addedToHierarchy, screenSystemEnabling);
			}
		}

		private void OnPageRequested(IDifficultyBeatmap difficultyBeatmap, IMapLeaderboardSource leaderboardSource, int page)
		{
			_ = OnPageRequestedAsync(difficultyBeatmap, leaderboardSource, page);
		}

		private async Task OnPageRequestedAsync(IDifficultyBeatmap difficultyBeatmap, IMapLeaderboardSource leaderboardSource, int page)
		{
			if (!Utils.IsDependencyLeaderboardInstalled)
			{
				return;
			}

			_leaderboardTokenSource?.Cancel();
			_leaderboardTokenSource?.Dispose();
			_leaderboardTokenSource = new CancellationTokenSource();
			var leaderboardEntries = await leaderboardSource.GetScoresAsync(difficultyBeatmap, _leaderboardTokenSource.Token, page);

			if (leaderboardEntries != null)
			{
				if (leaderboardEntries.Count == 0 || leaderboardEntries[0].CR.Count == 0)
				{
					leaderboardEntries = null;
				}
			}

			foreach (var leaderboardEntriesUpdater in _leaderboardEntriesUpdaters)
			{
				await UnityMainThreadTaskScheduler.Factory.StartNew(() => leaderboardEntriesUpdater.LeaderboardEntriesUpdated(leaderboardEntries));
			}
		}

		private void OnPoolChanged(string pool)
		{
			foreach (var poolUpdater in _poolUpdaters)
			{
				poolUpdater.PoolUpdated(pool);
			}

			_currentPool = pool;
		}

		private void OnRankTextClicked(HitbloqRankInfo rankInfo, string pool)
		{
			_hitbloqProfileModalController.ShowModalForSelf(_hitbloqLeaderboardViewController.transform, rankInfo, pool);
		}

		private void OnLogoClicked()
		{
			_hitbloqFlowCoordinator.ShowAndOpenPoolWithID(_currentPool);
		}

		private void OnEventClicked()
		{
			_hitbloqEventModalViewController.ShowModal(_hitbloqLeaderboardViewController.transform);
		}

		private void SceneManagerOnactiveSceneChanged(Scene currentScene, Scene nextScene)
		{
			if (currentScene.name == "MainMenu")
			{
				_scoreAlreadyUploaded = false;
			}
		}
	}
}