using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using Hitbloq.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using Zenject;
using LeaderboardCore.Interfaces;

namespace Hitbloq.Managers
{
    internal class HitbloqManager : IInitializable, IDisposable, INotifyScoreUpload
    {
        private readonly StandardLevelDetailViewController standardLevelDetailViewController;
        private readonly HitbloqLeaderboardViewController hitbloqLeaderboardViewController;
        private readonly HitbloqPanelController hitbloqPanelController;

        private readonly LevelInfoSource levelInfoSource;
        private readonly LeaderboardRefresher leaderboardRefresher;

        private readonly List<IDifficultyBeatmapUpdater> difficultyBeatmapUpdaters;
        private readonly List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters;
        private readonly List<IPoolUpdater> poolUpdaters;

        private CancellationTokenSource levelInfoTokenSource;
        private CancellationTokenSource leaderboardTokenSource;

        public HitbloqManager(StandardLevelDetailViewController standardLevelDetailViewController, HitbloqLeaderboardViewController hitbloqLeaderboardViewController, HitbloqPanelController hitbloqPanelController,
            LevelInfoSource levelInfoSource, LeaderboardRefresher leaderboardRefresher, List<IDifficultyBeatmapUpdater> difficultyBeatmapUpdaters, List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters, List<IPoolUpdater> poolUpdaters)
        {
            this.standardLevelDetailViewController = standardLevelDetailViewController;
            this.hitbloqLeaderboardViewController = hitbloqLeaderboardViewController;
            this.hitbloqPanelController = hitbloqPanelController;

            this.levelInfoSource = levelInfoSource;
            this.leaderboardRefresher = leaderboardRefresher;

            this.difficultyBeatmapUpdaters = difficultyBeatmapUpdaters;
            this.leaderboardEntriesUpdaters = leaderboardEntriesUpdaters;
            this.poolUpdaters = poolUpdaters;
        }

        public void Initialize()
        {
            standardLevelDetailViewController.didChangeDifficultyBeatmapEvent += OnDifficultyBeatmapChanged;
            standardLevelDetailViewController.didChangeContentEvent += OnContentChanged;
            hitbloqLeaderboardViewController.PageRequested += OnPageRequested;
            hitbloqPanelController.PoolChangedEvent += OnPoolChanged;
            hitbloqPanelController.ClickedRankText += OnRankTextClicked;
        }

        public void Dispose()
        {
            standardLevelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyBeatmapChanged;
            standardLevelDetailViewController.didChangeContentEvent -= OnContentChanged;
            hitbloqLeaderboardViewController.PageRequested -= OnPageRequested;
            hitbloqPanelController.PoolChangedEvent -= OnPoolChanged;
            hitbloqPanelController.ClickedRankText -= OnRankTextClicked;
        }

        public async void OnScoreUploaded()
        {
            if (await leaderboardRefresher.Refresh())
            {
                UpdateDifficultyBeatmap(standardLevelDetailViewController.selectedDifficultyBeatmap);
            }
        }

        private void OnDifficultyBeatmapChanged(StandardLevelDetailViewController _, IDifficultyBeatmap difficultyBeatmap) => UpdateDifficultyBeatmap(difficultyBeatmap);

        private void OnContentChanged(StandardLevelDetailViewController _, StandardLevelDetailViewController.ContentType contentType)
        {
            if (standardLevelDetailViewController.selectedDifficultyBeatmap != null && (contentType == StandardLevelDetailViewController.ContentType.OwnedAndReady || contentType == StandardLevelDetailViewController.ContentType.Inactive))
            {
                UpdateDifficultyBeatmap(standardLevelDetailViewController.selectedDifficultyBeatmap);
            }
        }

        private async void UpdateDifficultyBeatmap(IDifficultyBeatmap difficultyBeatmap)
        {
            levelInfoTokenSource?.Cancel();

            HitbloqLevelInfo levelInfoEntry;

            if (difficultyBeatmap.level is CustomPreviewBeatmapLevel)
            {
                levelInfoTokenSource = new CancellationTokenSource();
                levelInfoEntry = await levelInfoSource.GetLevelInfoAsync(difficultyBeatmap, levelInfoTokenSource.Token);
            }
            else
            {
                levelInfoEntry = null;
            }

            if (levelInfoEntry != null)
            {
                if (levelInfoEntry.pools.Count == 0)
                {
                    levelInfoEntry = null;
                }
            }

            foreach (var difficultyBeatmapUpdater in difficultyBeatmapUpdaters)
            {
                difficultyBeatmapUpdater.DifficultyBeatmapUpdated(standardLevelDetailViewController.selectedDifficultyBeatmap, levelInfoEntry);
            }
        }

        private async void OnPageRequested(IDifficultyBeatmap difficultyBeatmap, ILeaderboardSource leaderboardSource, int page)
        {
            leaderboardTokenSource?.Cancel();
            leaderboardTokenSource = new CancellationTokenSource();
            List<Entries.LeaderboardEntry> leaderboardEntries = await leaderboardSource.GetScoresTask(difficultyBeatmap, leaderboardTokenSource.Token, page);

            if (leaderboardEntries != null)
            {
                if (leaderboardEntries.Count == 0 || leaderboardEntries[0].cr.Count == 0)
                {
                    leaderboardEntries = null;
                }
            }

            foreach(var leaderboardEntriesUpdater in leaderboardEntriesUpdaters)
            {
                leaderboardEntriesUpdater.LeaderboardEntriesUpdated(leaderboardEntries);
            }
        }

        private void OnPoolChanged(string pool)
        {
            foreach (var poolUpdater in poolUpdaters)
            {
                poolUpdater.PoolUpdated(pool);
            }
        }

        private void OnRankTextClicked()
        {
            hitbloqLeaderboardViewController.ShowModal();
        }
    }
}
