using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Sources;
using Hitbloq.UI;
using Hitbloq.UI.ViewControllers;
using Hitbloq.Utilities;
using IPA.Utilities.Async;
using LeaderboardCore.Interfaces;
using LeaderboardCore.Utilities;
using UnityEngine.SceneManagement;
using Zenject;

namespace Hitbloq.Managers
{
	internal class HitbloqManager : IInitializable, IDisposable, INotifyLeaderboardSet
	{
		// Edited by GPT-5 Codex 2026-05-27
		// The refresh starts shortly after an upload event so the leaderboard updates faster.
		// The later queue polling still handles server-side processing time.
		private const int ScoreUploadRefreshDelayMilliseconds = 500;

		private readonly List<IBeatmapKeyUpdater> _beatmapKeyUpdaters;
		private readonly HitbloqEventModalViewController _hitbloqEventModalViewController;
		private readonly HitbloqFlowCoordinator _hitbloqFlowCoordinator;
		private readonly HitbloqLeaderboardViewController _hitbloqLeaderboardViewController;
		private readonly HitbloqPanelController _hitbloqPanelController;
		private readonly HitbloqProfileModalController _hitbloqProfileModalController;
		private readonly object _scoreUploadRefreshLock = new();
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
		private CancellationTokenSource? _scoreUploadRefreshTokenSource;
		private int _leaderboardRequestVersion;
		private bool _scoreAlreadyUploaded;
		private bool _scoreUploadRefreshInProgress;
		private bool _selectedMapRankedOnAnyPool;
		private int _scoreUploadRefreshVersion;

		private BeatmapKey? _selectedBeatmapKey;

		public HitbloqManager(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, HitbloqPanelController hitbloqPanelController, HitbloqProfileModalController hitbloqProfileModalController, HitbloqEventModalViewController hitbloqEventModalViewController, HitbloqFlowCoordinator hitbloqFlowCoordinator, UserIDSource userIDSource, LevelInfoSource levelInfoSource, LeaderboardRefresher leaderboardRefresher, List<INotifyUserRegistered> notifyUserRegistereds, List<IBeatmapKeyUpdater> beatmapKeyUpdaters, List<INotifyViewActivated> notifyViewActivateds, List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters, List<IPoolUpdater> poolUpdaters)
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
			_beatmapKeyUpdaters = beatmapKeyUpdaters;
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

			_scoreUploadRefreshTokenSource?.Cancel();
			_scoreUploadRefreshTokenSource?.Dispose();
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

		public void OnLeaderboardSet(
#if HITBLOQ_BS_1_29_1
			IDifficultyBeatmap difficultyBeatmap
#else
			BeatmapKey beatmapKey
#endif
		)
		{
#if HITBLOQ_BS_1_29_1
			var beatmapKey = BeatmapKey.FromDifficultyBeatmap(difficultyBeatmap);
#endif
			_ = OnLeaderboardSetAsync(beatmapKey);
		}

		public void OnScoreUploaded()
		{
			CancellationToken cancellationToken;
			int scoreUploadRefreshVersion;

			lock (_scoreUploadRefreshLock)
			{
				// Edited by GPT-5 Codex 2026-05-28
				// BeatLeader can report local replay saves even when Hitbloq has no pool for the map.
				// Skip refresh work unless the current map is ranked on at least one Hitbloq pool.
				if (!_selectedMapRankedOnAnyPool)
				{
					return;
				}

				if (_scoreAlreadyUploaded || _scoreUploadRefreshInProgress)
				{
					return;
				}

				_scoreUploadRefreshInProgress = true;
				_scoreUploadRefreshTokenSource?.Cancel();
				_scoreUploadRefreshTokenSource?.Dispose();
				_scoreUploadRefreshTokenSource = new CancellationTokenSource();
				cancellationToken = _scoreUploadRefreshTokenSource.Token;
				scoreUploadRefreshVersion = _scoreUploadRefreshVersion;
			}

			_ = OnScoreUploadAsync(scoreUploadRefreshVersion, cancellationToken);
		}

		private async Task OnScoreUploadAsync(int scoreUploadRefreshVersion, CancellationToken cancellationToken)
		{
			try
			{
				await Task.Delay(ScoreUploadRefreshDelayMilliseconds, cancellationToken);

				if (cancellationToken.IsCancellationRequested || scoreUploadRefreshVersion != _scoreUploadRefreshVersion)
				{
					return;
				}

				var refreshSuccessful = await _leaderboardRefresher.Refresh();

				if (!cancellationToken.IsCancellationRequested && scoreUploadRefreshVersion == _scoreUploadRefreshVersion && !_scoreAlreadyUploaded && refreshSuccessful)
				{
					_scoreAlreadyUploaded = true;
					await OnLeaderboardSetAsync(_selectedBeatmapKey);
				}
			}
			catch (TaskCanceledException)
			{
			}
			finally
			{
				lock (_scoreUploadRefreshLock)
				{
					if (scoreUploadRefreshVersion == _scoreUploadRefreshVersion)
					{
						_scoreUploadRefreshInProgress = false;
					}
				}
			}
		}

		private async Task OnLeaderboardSetAsync(BeatmapKey? beatmapKey)
		{
			if (beatmapKey != null)
			{
				_selectedBeatmapKey = beatmapKey;
				HitbloqLevelInfo? levelInfoEntry = null;

				if (beatmapKey.Value.levelId.Contains("custom_level_"))
				{
					_levelInfoTokenSource?.Cancel();
					_levelInfoTokenSource?.Dispose();
					_levelInfoTokenSource = new CancellationTokenSource();
					levelInfoEntry = await _levelInfoSource.GetLevelInfoAsync(beatmapKey.Value, _levelInfoTokenSource.Token);
				}

				if (levelInfoEntry != null)
				{
					if (levelInfoEntry.Pools.Count == 0)
					{
						levelInfoEntry = null;
					}
				}

				// Edited by GPT-5 Codex 2026-05-28
				// Upload refresh should only run for maps Hitbloq can actually score.
				// The upload callback reads this cached state before scheduling a refresh.
				_selectedMapRankedOnAnyPool = levelInfoEntry != null;
				
				foreach (var beatmapKeyUpdater in _beatmapKeyUpdaters)
				{
					await UnityMainThreadTaskScheduler.Factory.StartNew(() => beatmapKeyUpdater.BeatmapKeyUpdated(beatmapKey.Value, levelInfoEntry));
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

		private void OnPageRequested(BeatmapKey beatmapKey, IMapLeaderboardSource leaderboardSource, int page)
		{
			_ = OnPageRequestedAsync(beatmapKey, leaderboardSource, page);
		}

		private async Task OnPageRequestedAsync(BeatmapKey beatmapKey, IMapLeaderboardSource leaderboardSource, int page)
		{
			if (!Utils.IsDependencyLeaderboardInstalled)
			{
				return;
			}

			_leaderboardTokenSource?.Cancel();
			_leaderboardTokenSource?.Dispose();
			_leaderboardTokenSource = new CancellationTokenSource();
			var requestVersion = ++_leaderboardRequestVersion;
			var leaderboardToken = _leaderboardTokenSource.Token;
			var leaderboardEntries = await leaderboardSource.GetScoresAsync(beatmapKey, leaderboardToken, page);

			if (leaderboardToken.IsCancellationRequested || requestVersion != _leaderboardRequestVersion)
			{
				return;
			}

			if (leaderboardEntries != null)
			{
				if (leaderboardEntries.Count == 0 || leaderboardEntries[0].CR.Count == 0)
				{
					leaderboardEntries = null;
				}
			}

			foreach (var leaderboardEntriesUpdater in _leaderboardEntriesUpdaters)
			{
				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					if (leaderboardToken.IsCancellationRequested || requestVersion != _leaderboardRequestVersion)
					{
						return;
					}

					leaderboardEntriesUpdater.LeaderboardEntriesUpdated(leaderboardEntries);
				});
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
				lock (_scoreUploadRefreshLock)
				{
					_scoreUploadRefreshVersion++;
					_scoreUploadRefreshTokenSource?.Cancel();
					_scoreAlreadyUploaded = false;
					_scoreUploadRefreshInProgress = false;
				}
			}
		}
	}
}
