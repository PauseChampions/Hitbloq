using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using Hitbloq.UI;
using System;
using System.Collections.Generic;
using System.Threading;
using Zenject;

namespace Hitbloq.Managers
{
    internal class HitbloqDataManager : IInitializable, IDisposable
    {
        private readonly StandardLevelDetailViewController standardLevelDetailViewController;
        private readonly HitbloqLeaderboardViewController hitbloqLeaderboardViewController;

        private readonly List<IDifficultyBeatmapUpdater> difficultyBeatmapUpdaters;
        private readonly List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters;

        private readonly LevelInfoSource levelInfoSource;

        private CancellationTokenSource levelInfoTokenSource;
        private CancellationTokenSource leaderboardTokenSource;

        public HitbloqDataManager(StandardLevelDetailViewController standardLevelDetailViewController, HitbloqLeaderboardViewController hitbloqLeaderboardViewController, 
            LevelInfoSource levelInfoSource, List<IDifficultyBeatmapUpdater> difficultyBeatmapUpdaters, List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters)
        {
            this.standardLevelDetailViewController = standardLevelDetailViewController;
            this.hitbloqLeaderboardViewController = hitbloqLeaderboardViewController;

            this.levelInfoSource = levelInfoSource;

            this.difficultyBeatmapUpdaters = difficultyBeatmapUpdaters;
            this.leaderboardEntriesUpdaters = leaderboardEntriesUpdaters;
        }

        public void Initialize()
        {
            standardLevelDetailViewController.didChangeDifficultyBeatmapEvent += OnDifficultyBeatmapChanged;
            standardLevelDetailViewController.didChangeContentEvent += OnContentChanged;
            hitbloqLeaderboardViewController.PageRequested += OnPageRequested;
        }

        public void Dispose()
        {
            standardLevelDetailViewController.didChangeDifficultyBeatmapEvent -= OnDifficultyBeatmapChanged;
            standardLevelDetailViewController.didChangeContentEvent -= OnContentChanged;
            hitbloqLeaderboardViewController.PageRequested -= OnPageRequested;
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
            levelInfoTokenSource = new CancellationTokenSource();
            HitbloqLevelInfo levelInfoEntry = await levelInfoSource.GetLevelInfoAsync(difficultyBeatmap, levelInfoTokenSource.Token);

            if (levelInfoEntry != null)
            {
                if (levelInfoEntry.pools.Count == 0)
                {
                    levelInfoEntry = null;
                }
            }

            if (!levelInfoTokenSource.IsCancellationRequested)
            {
                foreach (var difficultyBeatmapUpdater in difficultyBeatmapUpdaters)
                {
                    difficultyBeatmapUpdater.DifficultyBeatmapUpdated(standardLevelDetailViewController.selectedDifficultyBeatmap, levelInfoEntry);
                }
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
    }
}
