using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Other;
using Hitbloq.Pages;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using Tweening;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPoolLeaderboardView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPoolLeaderboardView.bsml")]
    internal class HitbloqPoolLeaderboardViewController : BSMLAutomaticViewController, TableView.IDataSource
    {
        [Inject]
        private readonly HitbloqProfileModalController profileModalController = null!;
        
        [Inject]
        private readonly List<IPoolLeaderboardSource> leaderboardSources = null!;
        
        [Inject]
        private readonly UserIDSource userIDSource = null!;
        
        [Inject]
        private readonly SpriteLoader spriteLoader = null!;
        
        [Inject]
        private readonly MaterialGrabber materialGrabber = null!;
        
        [Inject]
        private readonly TimeTweeningManager uwuTweenyManager = null!;
        
        private readonly List<HitbloqPoolLeaderboardEntry> leaderboardEntries = new();
        private PoolLeaderboardPage? currentPage;
        private CancellationTokenSource? cancellationTokenSource;
        
        private ScrollView? scrollView;
        private float? currentScrollPosition;
        
        private readonly SemaphoreSlim leaderboardLoadSemaphore = new(1, 1);

        [UIComponent("list")]
        private readonly CustomListTableData? customListTableData = null!;
        
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (scrollView != null)
            {
                scrollView.scrollPositionChangedEvent += OnScrollPositionChanged;
            }
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            if (scrollView != null)
            {
                scrollView.scrollPositionChangedEvent -= OnScrollPositionChanged;
            }
        }
        
        public void SetPool(string poolID)
        {
            if (customListTableData == null)
            {
                return;
            }
            
            cancellationTokenSource?.Cancel();
            cancellationTokenSource?.Dispose();
            cancellationTokenSource = new CancellationTokenSource();

            _ = InitLeaderboard(poolID, cancellationTokenSource.Token, true);
        }
        
        private void OnScrollPositionChanged(float newPos)
        {
            if (scrollView == null || currentPage == null || currentPage.ExhaustedPages || currentScrollPosition == newPos || leaderboardLoadSemaphore.CurrentCount == 0)
            {
                return;
            }
            
            currentScrollPosition = newPos;
            scrollView.RefreshButtons();

            if (!Accessors.PageDownAccessor(ref scrollView).interactable)
            {
                cancellationTokenSource?.Cancel();
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = new CancellationTokenSource();
                _ = InitLeaderboard("", cancellationTokenSource.Token, false);
            }
        }
        
        [UIAction("#post-parse")]
        private void PostParse()
        {
            if (customListTableData != null)
            {
                customListTableData.tableView.SetDataSource(this, true);
                scrollView = Accessors.ScrollViewAccessor(ref customListTableData.tableView);
            }
        }

        [UIAction("list-select")]
        private void Select(TableView tableView, int idx)
        { 
            tableView.ClearSelection();
            if (currentPage != null)
            {
                profileModalController.ShowModalForUser(transform, leaderboardEntries[idx].UserID, currentPage.poolID);
            }
        }
        
        private async Task InitLeaderboard(string poolID, CancellationToken cancellationToken, bool firstPage)
        {
            if (customListTableData == null)
            {
                return;
            }

            await leaderboardLoadSemaphore.WaitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            try
            {
                if (firstPage)
                {
                    await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                    {
                        leaderboardEntries.Clear();
                        customListTableData.tableView.ClearSelection();
                        customListTableData.tableView.ReloadData();
                        Loaded = false;
                    });   
                }

                // We check the cancellationtoken at each interval instead of running everything with a single token
                // due to unity not liking it
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                PoolLeaderboardPage? page;

                if (firstPage || currentPage == null)
                {
                    page = await leaderboardSources[SelectedCellIndex].GetScoresAsync(poolID, cancellationToken, 0);
                }
                else
                {
                    page = await currentPage.Next(cancellationToken);
                }
            
                if (cancellationToken.IsCancellationRequested || page == null)
                {
                    return;
                }

                currentPage = page;
                leaderboardEntries.AddRange(currentPage.Entries);
            }
            finally
            {
                Loaded = true;
                await SiraUtil.Extras.Utilities.PauseChamp;
                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    customListTableData.tableView.ReloadDataKeepingPosition();
                });
                leaderboardLoadSemaphore.Release();

                if (firstPage && currentPage is {ExhaustedPages: false})
                {
                    // Request another page if on first
                    _ = InitLeaderboard("", cancellationToken, false);
                }
            }
        }

        #region Segmented Control

        private int selectedCellIndex;
        private int SelectedCellIndex
        {
            get => selectedCellIndex;
            set
            {
                selectedCellIndex = value;
                if (currentPage != null)
                {
                    cancellationTokenSource?.Cancel();
                    cancellationTokenSource?.Dispose();
                    cancellationTokenSource = new CancellationTokenSource();
                    _ = InitLeaderboard(currentPage.poolID, cancellationTokenSource.Token, true);
                }
            }
        }
        
        [UIAction("cell-selected")]
        private void OnCellSelected(SegmentedControl _, int index)
        {
            SelectedCellIndex = index;
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

        #endregion
        
        #region Loading

        private bool loaded;

        [UIValue("is-loading")]
        public bool IsLoading => !Loaded;

        [UIValue("has-loaded")]
        public bool Loaded
        {
            get => loaded;
            set
            {
                loaded = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsLoading));
            }
        }

        #endregion

        #region TableData

        private const string kReuseIdentifier = "HitbloqPoolLeaderboardCell";
        
        private HitbloqPoolLeaderboardCellController GetCell()
        {
            var tableCell = customListTableData!.tableView.DequeueReusableCellForIdentifier(kReuseIdentifier);

            if (tableCell == null)
            {
                var hitbloqPoolLeaderboardCell = new GameObject(nameof(HitbloqPoolLeaderboardCellController), typeof(Touchable)).AddComponent<HitbloqPoolLeaderboardCellController>();
                hitbloqPoolLeaderboardCell.SetRequiredUtils(userIDSource, spriteLoader, materialGrabber, uwuTweenyManager);
                tableCell = hitbloqPoolLeaderboardCell;
                tableCell.interactable = true;

                tableCell.reuseIdentifier = kReuseIdentifier;
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqPoolLeaderboardCell.bsml"), tableCell.gameObject, tableCell);
            }

            return (HitbloqPoolLeaderboardCellController) tableCell;
        }

        public float CellSize() => 7;

        public int NumberOfCells() => leaderboardEntries.Count;

        public TableCell CellForIdx(TableView tableView, int idx) => GetCell().PopulateCell(leaderboardEntries[idx]);

        #endregion
    }
}