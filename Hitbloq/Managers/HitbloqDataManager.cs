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

        private CancellationTokenSource tokenSource;

        public HitbloqDataManager(StandardLevelDetailViewController standardLevelDetailViewController, HitbloqLeaderboardViewController hitbloqLeaderboardViewController, 
            List<IDifficultyBeatmapUpdater> difficultyBeatmapUpdaters, List<ILeaderboardEntriesUpdater> leaderboardEntriesUpdaters)
        {
            this.standardLevelDetailViewController = standardLevelDetailViewController;
            this.hitbloqLeaderboardViewController = hitbloqLeaderboardViewController;

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

        private void OnDifficultyBeatmapChanged(StandardLevelDetailViewController _, IDifficultyBeatmap difficultyBeatmap)
        {
            foreach(var difficultyBeatmapUpdater in difficultyBeatmapUpdaters)
            {
                difficultyBeatmapUpdater.DifficultyBeatmapUpdated(difficultyBeatmap);
            }
        }

        private void OnContentChanged(StandardLevelDetailViewController _, StandardLevelDetailViewController.ContentType contentType)
        {
            if (standardLevelDetailViewController.selectedDifficultyBeatmap != null && (contentType == StandardLevelDetailViewController.ContentType.OwnedAndReady || contentType == StandardLevelDetailViewController.ContentType.Inactive))
            {
                foreach (var difficultyBeatmapUpdater in difficultyBeatmapUpdaters)
                {
                    difficultyBeatmapUpdater.DifficultyBeatmapUpdated(standardLevelDetailViewController.selectedDifficultyBeatmap);
                }
            }
        }

        private async void OnPageRequested(IDifficultyBeatmap difficultyBeatmap, ILeaderboardSource leaderboardSource, int page)
        {
            tokenSource?.Cancel();
            tokenSource = new CancellationTokenSource();
            List<Entries.LeaderboardEntry> leaderboardEntries = await leaderboardSource.GetScoresTask(difficultyBeatmap, tokenSource.Token, page);

            foreach(var leaderboardEntriesUpdater in leaderboardEntriesUpdaters)
            {
                leaderboardEntriesUpdater.LeaderboardEntriesUpdated(leaderboardEntries);
            }
        }
    }
}
