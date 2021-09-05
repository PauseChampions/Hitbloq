using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqMainLeaderboardView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqMainLeaderboardView.bsml")]
    internal class HitbloqLeaderboardViewController : BSMLAutomaticViewController, IDifficultyBeatmapUpdater, ILeaderboardEntriesUpdater, IPoolUpdater
    {
        private UserInfoSource userInfoSource;
        private List<ILeaderboardSource> leaderboardSources;

        public event Action<IDifficultyBeatmap, ILeaderboardSource, int> PageRequested;

        private int _pageNumber;
        private int _selectedCellIndex;

        private IDifficultyBeatmap difficultyBeatmap;
        private List<Entries.LeaderboardEntry> leaderboardEntries;
        private string pool;

        private int PageNumber
        {
            get => _pageNumber;
            set
            {
                _pageNumber = value;
                NotifyPropertyChanged(nameof(UpEnabled));
                if (leaderboardTransform != null)
                {
                    leaderboard.SetScores(new List<LeaderboardTableView.ScoreData>(), 0);
                    leaderboardTransform.Find("LoadingControl").gameObject.SetActive(true);
                }
                PageRequested?.Invoke(difficultyBeatmap, leaderboardSources[SelectedCellIndex], value);
            }
        }

        private int SelectedCellIndex
        {
            get => _selectedCellIndex;
            set
            {
                _selectedCellIndex = value;
                PageNumber = 0;
            }
        }

        [UIComponent("leaderboard")]
        private Transform leaderboardTransform;

        [UIComponent("leaderboard")]
        internal LeaderboardTableView leaderboard;

        [Inject]
        private void Inject(UserInfoSource userInfoSource, List<ILeaderboardSource> leaderboardSources)
        {
            this.userInfoSource = userInfoSource;
            this.leaderboardSources = leaderboardSources;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (firstActivation)
            {
                List<LeaderboardTableView.ScoreData> placeholder = new List<LeaderboardTableView.ScoreData>();
                for (int i = 0; i < 10; i++)
                {
                    placeholder.Add(new LeaderboardTableView.ScoreData(0, "", 0, false));
                }

                LeaderboardTableCell[] leaderboardTableCells = leaderboardTransform.GetComponentsInChildren<LeaderboardTableCell>(true);
                foreach (var leaderboardTableCell in leaderboardTableCells)
                {
                    leaderboardTableCell.transform.Find("PlayerName").GetComponent<CurvedTextMeshPro>().richText = true;
                }
                Destroy(leaderboardTransform.Find("LoadingControl").Find("LoadingContainer").Find("Text").gameObject);
            }
            PageNumber = 0;
        }

        public async void SetScores(List<Entries.LeaderboardEntry> leaderboardEntries)
        {
            List<LeaderboardTableView.ScoreData> scores = new List<LeaderboardTableView.ScoreData>();
            int myScorePos = -1;

            if (leaderboardEntries == null || leaderboardEntries.Count == 0)
            {
                scores.Add(new LeaderboardTableView.ScoreData(0, "You haven't set a score on this leaderboard - <size=75%>(<color=#FFD42A>0%</color>)</size>", 0, false));
            }
            else
            {
                HitbloqUserInfo userInfo = await userInfoSource.GetUserInfoAsync();
                int id = userInfo.id;

                for(int i = 0; i < leaderboardEntries.Count; i++)
                {
                    scores.Add(new LeaderboardTableView.ScoreData(leaderboardEntries[i].score, $"<size=85%>{leaderboardEntries[i].username} - <size=75%>(<color=#FFD42A>{leaderboardEntries[i].accuracy.ToString("F2")}%</color>)</size></size> - <size=75%> (<color=#6772E5>{leaderboardEntries[i].cr[pool].ToString("F2")}<size=45%>cr</size></color>)</size>", 
                        leaderboardEntries[i].rank, false));
                    if (leaderboardEntries[i].userID == id)
                    {
                        myScorePos = i;
                    }
                }
            }

            if (leaderboardTransform != null)
            {
                leaderboardTransform.Find("LoadingControl").gameObject.SetActive(false);
                leaderboard.SetScores(scores, myScorePos);
            }
        }

        [UIAction("cell-selected")]
        private void OnCellSelected(SegmentedControl _, int index)
        {
            SelectedCellIndex = index;
        }

        [UIAction("up-clicked")]
        private void UpClicked()
        {
            if (UpEnabled)
            {
                PageNumber--;
            }
        }

        [UIAction("down-clicked")]
        private void DownClicked()
        {
            if (DownEnabled)
            {
                PageNumber++;
            }
        }

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo levelInfoEntry)
        {
            if (levelInfoEntry != null)
            {
                this.difficultyBeatmap = difficultyBeatmap;
                pool = levelInfoEntry.pools.Keys.First();
                if (isActiveAndEnabled)
                {
                    PageNumber = 0;
                }
            }
        }

        public void LeaderboardEntriesUpdated(List<Entries.LeaderboardEntry> leaderboardEntries)
        {
            this.leaderboardEntries = leaderboardEntries;
            NotifyPropertyChanged(nameof(DownEnabled));
            SetScores(leaderboardEntries);
        }

        public void PoolUpdated(string pool)
        {
            this.pool = pool;
            SetScores(leaderboardEntries);
        }

        [UIValue("cell-data")]
        private List<IconSegmentedControl.DataItem> cellData
        {
            get
            {
                List<IconSegmentedControl.DataItem> list = new List<IconSegmentedControl.DataItem>();
                foreach (var leaderboardSource in leaderboardSources)
                {
                    list.Add(new IconSegmentedControl.DataItem(leaderboardSource.Icon, leaderboardSource.HoverHint));
                }
                return list;
            }
        }

        [UIValue("up-enabled")]
        private bool UpEnabled => PageNumber != 0 && leaderboardSources[SelectedCellIndex].Scrollable;

        [UIValue("down-enabled")]
        private bool DownEnabled => leaderboardEntries != null && leaderboardEntries.Count == 10 && leaderboardSources[SelectedCellIndex].Scrollable;
    }
}
