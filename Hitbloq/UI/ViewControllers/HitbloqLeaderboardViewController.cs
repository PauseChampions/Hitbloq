using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using HMUI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqLeaderboardView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqLeaderboardView.bsml")]
    internal class HitbloqLeaderboardViewController : BSMLAutomaticViewController, IDifficultyBeatmapUpdater, ILeaderboardEntriesUpdater, IPoolUpdater
    {
        [Inject]
        private readonly HitbloqProfileModalController profileModalController = null!;
        
        [Inject]
        private readonly UserIDSource userIDSource = null!;
        
        [Inject]
        private readonly List<ILeaderboardSource> leaderboardSources = null!;

        public event Action<IDifficultyBeatmap, ILeaderboardSource, int>? PageRequested;

        private int pageNumber;
        private int selectedCellIndex;

        private IDifficultyBeatmap? difficultyBeatmap;
        private List<HitbloqLeaderboardEntry>? leaderboardEntries;
        private string? selectedPool;

        private List<Button>? infoButtons;

        private int PageNumber
        {
            get => pageNumber;
            set
            {
                pageNumber = value;
                NotifyPropertyChanged(nameof(UpEnabled));
                if (leaderboard != null && loadingControl != null && difficultyBeatmap != null)
                {
                    leaderboard.SetScores(new List<LeaderboardTableView.ScoreData>(), 0);
                    loadingControl.SetActive(true);
                    PageRequested?.Invoke(difficultyBeatmap, leaderboardSources[SelectedCellIndex], value);
                }
            }
        }

        private int SelectedCellIndex
        {
            get => selectedCellIndex;
            set
            {
                selectedCellIndex = value;
                PageNumber = 0;
            }
        }

        [UIComponent("leaderboard")]
        private readonly Transform? leaderboardTransform = null!;

        [UIComponent("leaderboard")]
        private readonly LeaderboardTableView? leaderboard = null!;

        private GameObject? loadingControl;

        #region Info Buttons

        [UIComponent("button1")] 
        private readonly Button? button1 = null!;

        [UIComponent("button2")]
        private readonly Button? button2 = null!;

        [UIComponent("button3")]
        private readonly Button? button3 = null!;

        [UIComponent("button4")]
        private readonly Button? button4 = null!;

        [UIComponent("button5")]
        private readonly Button? button5 = null!;

        [UIComponent("button6")]
        private readonly Button? button6 = null!;

        [UIComponent("button7")]
        private readonly Button? button7 = null!;

        [UIComponent("button8")]
        private readonly Button? button8 = null!;

        [UIComponent("button9")]
        private readonly Button? button9 = null!;

        [UIComponent("button10")]
        private readonly Button? button10 = null!;

        #endregion

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            foreach (var leaderboardSource in leaderboardSources)
            {
                leaderboardSource.ClearCache();
            }
            PageNumber = 0;
        }

        private async Task SetScores(List<HitbloqLeaderboardEntry>? leaderboardEntries)
        {
            var scores = new List<LeaderboardTableView.ScoreData>();
            var myScorePos = -1;

            if (infoButtons != null)
            {
                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    foreach (var button in infoButtons)
                    {
                        button.gameObject.SetActive(false);
                    }
                });
            }

            if (leaderboardEntries == null || leaderboardEntries.Count == 0)
            {
                scores.Add(new LeaderboardTableView.ScoreData(0, "You haven't set a score on this leaderboard - <size=75%>(<color=#FFD42A>0%</color>)</size>", 0, false));
            }
            else
            {
                if (selectedPool == null || !leaderboardEntries.First().CR.ContainsKey(selectedPool))
                {
                    return;
                }

                var userID = await userIDSource.GetUserIDAsync();
                var id = userID?.ID ?? -1;

                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    for (var i = 0; i < (leaderboardEntries.Count > 10 ? 10 : leaderboardEntries.Count); i++)
                    {
                        scores.Add(new LeaderboardTableView.ScoreData(leaderboardEntries[i].Score,
                            $"<color={leaderboardEntries[i].CustomColor ?? "#ffffff"}><size=85%>{leaderboardEntries[i].Username}</color> - <size=75%>(<color=#FFD42A>{leaderboardEntries[i].Accuracy.ToString("F2")}%</color>)</size></size> - <size=75%> (<color=#aa6eff>{leaderboardEntries[i].CR[selectedPool].ToString("F2")}<size=55%>cr</size></color>)</size>",
                            leaderboardEntries[i].Rank, false));

                        if (infoButtons != null)
                        {
                            infoButtons[i].gameObject.SetActive(true);
                            var hoverHint = infoButtons[i].GetComponent<HoverHint>();
                            hoverHint.text = $"Score Set: {leaderboardEntries[i].DateSet}";
                        }

                        if (leaderboardEntries[i].UserID == id)
                        {
                            myScorePos = i;
                        }
                    }
                });
            }

            if (loadingControl != null && leaderboard != null)
            {
                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    loadingControl.SetActive(false);
                    leaderboard.SetScores(scores, myScorePos);
                });
            }
        }

        private void ChangeButtonScale(Button button, float scale)
        {
            var transform = button.transform;
            var localScale = transform.localScale;
            transform.localScale = localScale * scale;
            infoButtons?.Add(button);
        }

        public void InfoButtonClicked(int index)
        {
            if (leaderboardEntries != null && index < leaderboardEntries.Count && selectedPool != null)
            {
                profileModalController.ShowModalForUser(transform, leaderboardEntries[index].UserID, selectedPool);
            }
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            // To set rich text, I have to iterate through all cells, set each cell to allow rich text and next time they will have it
            var leaderboardTableCells = leaderboardTransform!.GetComponentsInChildren<LeaderboardTableCell>(true);
            foreach (var leaderboardTableCell in leaderboardTableCells)
            {
                leaderboardTableCell.transform.Find("PlayerName").GetComponent<CurvedTextMeshPro>().richText = true;
            }
            
            loadingControl = leaderboardTransform.Find("LoadingControl").gameObject;

            var loadingContainer = loadingControl.transform.Find("LoadingContainer");
            loadingContainer.gameObject.SetActive(true);
            Destroy(loadingContainer.Find("Text").gameObject);
            Destroy(loadingControl.transform.Find("RefreshContainer").gameObject);
            Destroy(loadingControl.transform.Find("DownloadingContainer").gameObject);

            infoButtons = new List<Button>();

            // Change info button scales
            ChangeButtonScale(button1!, 0.425f);
            ChangeButtonScale(button2!, 0.425f);
            ChangeButtonScale(button3!, 0.425f);
            ChangeButtonScale(button4!, 0.425f);
            ChangeButtonScale(button5!, 0.425f);
            ChangeButtonScale(button6!, 0.425f);
            ChangeButtonScale(button7!, 0.425f);
            ChangeButtonScale(button8!, 0.425f);
            ChangeButtonScale(button9!, 0.425f);
            ChangeButtonScale(button10!, 0.425f);
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

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo? levelInfoEntry)
        {
            if (levelInfoEntry != null)
            {
                this.difficultyBeatmap = difficultyBeatmap;
                if (isActiveAndEnabled)
                {
                    foreach (var leaderboardSource in leaderboardSources)
                    {
                        leaderboardSource.ClearCache();
                    }
                    PageNumber = 0;
                }
            }
        }

        public void LeaderboardEntriesUpdated(List<HitbloqLeaderboardEntry>? leaderboardEntries)
        {
            this.leaderboardEntries = leaderboardEntries;
            NotifyPropertyChanged(nameof(DownEnabled));
            _ = SetScores(leaderboardEntries);
        }

        public void PoolUpdated(string pool)
        {
            this.selectedPool = pool;
            if (isActiveAndEnabled)
            {
                _ = SetScores(leaderboardEntries);
            }
        }

        [UIValue("cell-data")]
        private List<IconSegmentedControl.DataItem> CellData
        {
            get
            {
                var list = new List<IconSegmentedControl.DataItem>();
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
        private bool DownEnabled => leaderboardEntries is {Count: 10} && leaderboardSources[SelectedCellIndex].Scrollable;
    }
}
