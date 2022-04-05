using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using Hitbloq.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Zenject;
using LeaderboardCore.Interfaces;
using Hitbloq.Other;

namespace Hitbloq.Managers
{
    internal class HitbloqManager : IInitializable, IDisposable, INotifyScoreUpload, INotifyLeaderboardSet
    {
        private readonly HitbloqLeaderboardViewController hitbloqLeaderboardViewController;
        private readonly HitbloqPanelController hitbloqPanelController;
        private readonly HitbloqProfileModalController hitbloqProfileModalController;
        private readonly HitbloqEventModalViewController hitbloqEventModalViewController;

        private readonly UserIDSource userIDSource;
        private readonly LevelInfoSource levelInfoSource;
        private readonly LeaderboardRefresher leaderboardRefresher;

        private readonly List<INotifyUserRegistered> notifyUserRegistereds;
        private readonly List<IDifficultyBeatmapUpdater> difficultyBeatmapUpdaters;
        private readonly List<INotifyViewActivated> notifyViewActivateds;
        private readonly List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters;
        private readonly List<IPoolUpdater> poolUpdaters;

        private IDifficultyBeatmap? selectedDifficultyBeatmap;
        private CancellationTokenSource? levelInfoTokenSource;
        private CancellationTokenSource? leaderboardTokenSource;

        public HitbloqManager(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, HitbloqPanelController hitbloqPanelController, HitbloqProfileModalController hitbloqProfileModalController,
            HitbloqEventModalViewController hitbloqEventModalViewController, UserIDSource userIDSource, LevelInfoSource levelInfoSource, LeaderboardRefresher leaderboardRefresher, List<INotifyUserRegistered> notifyUserRegistereds,
            List<IDifficultyBeatmapUpdater> difficultyBeatmapUpdaters, List<INotifyViewActivated> notifyViewActivateds, List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters,
            List<IPoolUpdater> poolUpdaters)
        {
            this.hitbloqLeaderboardViewController = hitbloqLeaderboardViewController;
            this.hitbloqPanelController = hitbloqPanelController;
            this.hitbloqProfileModalController = hitbloqProfileModalController;
            this.hitbloqEventModalViewController = hitbloqEventModalViewController;

            this.userIDSource = userIDSource;
            this.levelInfoSource = levelInfoSource;
            this.leaderboardRefresher = leaderboardRefresher;

            this.notifyUserRegistereds = notifyUserRegistereds;
            this.difficultyBeatmapUpdaters = difficultyBeatmapUpdaters;
            this.notifyViewActivateds = notifyViewActivateds;
            this.leaderboardEntriesUpdaters = leaderboardEntriesUpdaters;
            this.poolUpdaters = poolUpdaters;
        }

        public void Initialize()
        {
            userIDSource.UserRegisteredEvent += OnUserRegistered;

            hitbloqLeaderboardViewController.didActivateEvent += OnViewActivated;
            hitbloqLeaderboardViewController.PageRequested += OnPageRequested;

            hitbloqPanelController.PoolChangedEvent += OnPoolChanged;
            hitbloqPanelController.RankTextClickedEvent += OnRankTextClicked;
            hitbloqPanelController.LogoClickedEvent += OnLogoClicked;
        }

        public void Dispose()
        {
            userIDSource.UserRegisteredEvent -= OnUserRegistered;

            hitbloqLeaderboardViewController.didActivateEvent -= OnViewActivated;
            hitbloqLeaderboardViewController.PageRequested -= OnPageRequested;

            hitbloqPanelController.PoolChangedEvent -= OnPoolChanged;
            hitbloqPanelController.RankTextClickedEvent -= OnRankTextClicked;
            hitbloqPanelController.LogoClickedEvent -= OnLogoClicked;
        }

        public void OnScoreUploaded() => _ = OnScoreUploadAsync();
        private async Task OnScoreUploadAsync()
        {
            if (await leaderboardRefresher.Refresh())
            {
                await OnLeaderboardSetAsync(selectedDifficultyBeatmap);
            }
        }

        public void OnLeaderboardSet(IDifficultyBeatmap? difficultyBeatmap) =>
            _ = OnLeaderboardSetAsync(difficultyBeatmap);

        private async Task OnLeaderboardSetAsync(IDifficultyBeatmap? difficultyBeatmap)
        {
            if (difficultyBeatmap != null)
            {
                selectedDifficultyBeatmap = difficultyBeatmap;
                levelInfoTokenSource?.Cancel();
                levelInfoTokenSource?.Dispose();
                HitbloqLevelInfo? levelInfoEntry = null;

                if (difficultyBeatmap.level is CustomPreviewBeatmapLevel)
                {
                    levelInfoTokenSource = new CancellationTokenSource();
                    levelInfoEntry = await levelInfoSource.GetLevelInfoAsync(difficultyBeatmap, levelInfoTokenSource.Token);
                }

                if (levelInfoEntry != null)
                {
                    if (levelInfoEntry.Pools.Count == 0)
                    {
                        levelInfoEntry = null;
                    }
                }

                foreach (var difficultyBeatmapUpdater in difficultyBeatmapUpdaters)
                {
                    await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() => difficultyBeatmapUpdater.DifficultyBeatmapUpdated(difficultyBeatmap, levelInfoEntry));
                }
            }
        }

        private void OnUserRegistered()
        {
            foreach (var notifyUserRegistered in notifyUserRegistereds)
            {
                notifyUserRegistered.UserRegistered();
            }
        }

        private void OnViewActivated(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            foreach (var notifyViewActivated in notifyViewActivateds)
            {
                notifyViewActivated.ViewActivated(hitbloqLeaderboardViewController, firstActivation, addedToHierarchy, screenSystemEnabling);
            }
        }

        private void OnPageRequested(IDifficultyBeatmap difficultyBeatmap, ILeaderboardSource leaderboardSource, int page) =>
            _ = OnPageRequestedAsync(difficultyBeatmap, leaderboardSource, page);

        private async Task OnPageRequestedAsync(IDifficultyBeatmap difficultyBeatmap, ILeaderboardSource leaderboardSource, int page)
        {
            leaderboardTokenSource?.Cancel();
            leaderboardTokenSource?.Dispose();
            leaderboardTokenSource = new CancellationTokenSource();
            var leaderboardEntries = await leaderboardSource.GetScoresAsync(difficultyBeatmap, leaderboardTokenSource.Token, page);

            if (leaderboardEntries != null)
            {
                if (leaderboardEntries.Count == 0 || leaderboardEntries[0].CR.Count == 0)
                {
                    leaderboardEntries = null;
                }
            }

            foreach(var leaderboardEntriesUpdater in leaderboardEntriesUpdaters)
            {
                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() => leaderboardEntriesUpdater.LeaderboardEntriesUpdated(leaderboardEntries));
            }
        }

        private void OnPoolChanged(string pool)
        {
            foreach (var poolUpdater in poolUpdaters)
            {
                poolUpdater.PoolUpdated(pool);
            }
        }

        private void OnRankTextClicked(HitbloqRankInfo rankInfo, string pool)
        {
            hitbloqProfileModalController.ShowModalForSelf(hitbloqLeaderboardViewController.transform, rankInfo, pool);
        }

        private void OnLogoClicked()
        {
            hitbloqEventModalViewController.ShowModal(hitbloqLeaderboardViewController.transform);
        }
    }
}
