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
using UnityEngine.UI;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqLeaderboardView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqLeaderboardView.bsml")]
    internal class HitbloqLeaderboardViewController : BSMLAutomaticViewController, IDifficultyBeatmapUpdater, ILeaderboardEntriesUpdater, IPoolUpdater
    {
        private HitbloqProfileModalController profileModalController;
        private UserIDSource userIDSource;
        private List<ILeaderboardSource> leaderboardSources;

        public event Action<IDifficultyBeatmap, ILeaderboardSource, int> PageRequested;

        private int _pageNumber;
        private int _selectedCellIndex;

        private IDifficultyBeatmap difficultyBeatmap;
        private List<HitbloqLeaderboardEntry> leaderboardEntries;
        private string selectedPool;

        private List<Button> infoButtons;

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
                    loadingControl.SetActive(true);
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
        private readonly Transform leaderboardTransform;

        [UIComponent("leaderboard")]
        private readonly LeaderboardTableView leaderboard;

        private GameObject loadingControl;

        #region Info Buttons

        [UIComponent("button1")]
        protected Button button1;

        [UIComponent("button2")]
        protected Button button2;

        [UIComponent("button3")]
        protected Button button3;

        [UIComponent("button4")]
        protected Button button4;

        [UIComponent("button5")]
        protected Button button5;

        [UIComponent("button6")]
        protected Button button6;

        [UIComponent("button7")]
        protected Button button7;

        [UIComponent("button8")]
        protected Button button8;

        [UIComponent("button9")]
        protected Button button9;

        [UIComponent("button10")]
        protected Button button10;

        #endregion

        [Inject]
        private void Inject(HitbloqProfileModalController profileModalController, UserIDSource userIDSource, List<ILeaderboardSource> leaderboardSources)
        {
            this.profileModalController = profileModalController;
            this.userIDSource = userIDSource;
            this.leaderboardSources = leaderboardSources;
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            foreach (ILeaderboardSource leaderboardSource in leaderboardSources)
            {
                leaderboardSource.ClearCache();
            }
            PageNumber = 0;
        }

        public async void SetScores(List<HitbloqLeaderboardEntry> leaderboardEntries)
        {
            List<LeaderboardTableView.ScoreData> scores = new List<LeaderboardTableView.ScoreData>();
            int myScorePos = -1;

            if (infoButtons != null)
            {
                foreach (var button in infoButtons)
                {
                    button.gameObject.SetActive(false);
                }
            }

            if (leaderboardEntries == null || leaderboardEntries.Count == 0)
            {
                scores.Add(new LeaderboardTableView.ScoreData(0, "You haven't set a score on this leaderboard - <size=75%>(<color=#FFD42A>0%</color>)</size>", 0, false));
            }
            else
            {
                if (!leaderboardEntries.First().cr.ContainsKey(selectedPool))
                {
                    return;
                }

                HitbloqUserID userID = await userIDSource.GetUserIDAsync();
                int id = userID.id;

                for(int i = 0; i < (leaderboardEntries.Count > 10 ? 10: leaderboardEntries.Count); i++)
                {
                    scores.Add(new LeaderboardTableView.ScoreData(leaderboardEntries[i].score, $"<size=85%>{leaderboardEntries[i].username} - <size=75%>(<color=#FFD42A>{leaderboardEntries[i].accuracy.ToString("F2")}%</color>)</size></size> - <size=75%> (<color=#aa6eff>{leaderboardEntries[i].cr[selectedPool].ToString("F2")}<size=55%>cr</size></color>)</size>", 
                        leaderboardEntries[i].rank, false));

                    if (infoButtons != null)
                    {
                        infoButtons[i].gameObject.SetActive(true);
                        HoverHint hoverHint = infoButtons[i].GetComponent<HoverHint>();
                        hoverHint.text = $"Score Set: {leaderboardEntries[i].dateSet}";
                    }

                    if (leaderboardEntries[i].userID == id)
                    {
                        myScorePos = i;
                    }
                }
            }

            if (leaderboardTransform != null)
            {
                loadingControl.SetActive(false);
                leaderboard.SetScores(scores, myScorePos);
            }
        }

        private void ChangeButtonScale(Button button, float scale)
        {
            Transform transform = button.transform;
            Vector3 localScale = transform.localScale;
            transform.localScale = localScale * scale;
            infoButtons.Add(button);
        }

        public void InfoButtonClicked(int index)
        {
            if (index < leaderboardEntries.Count)
            {
                profileModalController.ShowModalForUser(transform, leaderboardEntries[index].userID, selectedPool);
            }
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            List<LeaderboardTableView.ScoreData> placeholder = new List<LeaderboardTableView.ScoreData>();
            for (int i = 0; i < 10; i++)
            {
                placeholder.Add(new LeaderboardTableView.ScoreData(0, "", 0, false));
            }

            // To set rich text, I have to make 10 empty cells, set each cell to allow rich text and next time they will have it
            LeaderboardTableCell[] leaderboardTableCells = leaderboardTransform.GetComponentsInChildren<LeaderboardTableCell>(true);
            foreach (var leaderboardTableCell in leaderboardTableCells)
            {
                leaderboardTableCell.transform.Find("PlayerName").GetComponent<CurvedTextMeshPro>().richText = true;
            }
            Destroy(leaderboardTransform.Find("LoadingControl").Find("LoadingContainer").Find("Text").gameObject);
            loadingControl = leaderboardTransform.Find("LoadingControl").gameObject;

            infoButtons = new List<Button>();

            // Change info button scales
            ChangeButtonScale(button1, 0.425f);
            ChangeButtonScale(button2, 0.425f);
            ChangeButtonScale(button3, 0.425f);
            ChangeButtonScale(button4, 0.425f);
            ChangeButtonScale(button5, 0.425f);
            ChangeButtonScale(button6, 0.425f);
            ChangeButtonScale(button7, 0.425f);
            ChangeButtonScale(button8, 0.425f);
            ChangeButtonScale(button9, 0.425f);
            ChangeButtonScale(button10, 0.425f);
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

        #region Info Buttons Clicked

        [UIAction("b-1-click")]
        private void B1Clicked() => InfoButtonClicked(0);

        [UIAction("b-2-click")]
        private void B2Clicked() => InfoButtonClicked(1);

        [UIAction("b-3-click")]
        private void B3Clicked() => InfoButtonClicked(2);

        [UIAction("b-4-click")]
        private void B4Clicked() => InfoButtonClicked(3);

        [UIAction("b-5-click")]
        private void B5Clicked() => InfoButtonClicked(4);

        [UIAction("b-6-click")]
        private void B6Clicked() => InfoButtonClicked(5);

        [UIAction("b-7-click")]
        private void B7Clicked() => InfoButtonClicked(6);

        [UIAction("b-8-click")]
        private void B8Clicked() => InfoButtonClicked(7);

        [UIAction("b-9-click")]
        private void B9Clicked() => InfoButtonClicked(8);

        [UIAction("b-10-click")]
        private void B10Clicked() => InfoButtonClicked(9);

        #endregion

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo levelInfoEntry)
        {
            if (levelInfoEntry != null)
            {
                this.difficultyBeatmap = difficultyBeatmap;
                if (isActiveAndEnabled)
                {
                    foreach (ILeaderboardSource leaderboardSource in leaderboardSources)
                    {
                        leaderboardSource.ClearCache();
                    }
                    PageNumber = 0;
                }
            }
        }

        public void LeaderboardEntriesUpdated(List<HitbloqLeaderboardEntry> leaderboardEntries)
        {
            this.leaderboardEntries = leaderboardEntries;
            NotifyPropertyChanged(nameof(DownEnabled));
            SetScores(leaderboardEntries);
        }

        public void PoolUpdated(string pool)
        {
            this.selectedPool = pool;
            if (isActiveAndEnabled)
            {
                SetScores(leaderboardEntries);
            }
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
