using System;
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
using Hitbloq.Sources;
using HMUI;
using Tweening;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPoolListView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPoolListView.bsml")]
    internal class HitbloqPoolListViewController : BSMLAutomaticViewController, TableView.IDataSource
    {
        [Inject]
        private readonly PoolListSource poolListSource = null!;
        
        [Inject]
        private readonly SpriteLoader spriteLoader = null!;
        
        [Inject]
        private readonly MaterialGrabber materialGrabber = null!;
        
        [Inject]
        private readonly TimeTweeningManager uwuTweenyManager = null!;
        
        [UIComponent("list")]
        private readonly CustomListTableData? customListTableData = null!;

        private readonly List<HitbloqPoolListEntry> pools = new();
        private readonly SemaphoreSlim poolLoadSemaphore = new(1, 1);
        private CancellationTokenSource? poolCancellationTokenSource;
        private CancellationTokenSource? sortCancellationTokenSource;

        public string? poolToOpen;

        public event Action<HitbloqPoolListEntry>? PoolSelectedEvent;
        public event Action? DetailDismissRequested;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            
            if (customListTableData != null)
            {
                customListTableData.tableView.ClearSelection();
            }
            
            if (pools.Count == 0)
            {
                poolCancellationTokenSource?.Cancel();
                poolCancellationTokenSource?.Dispose();
                poolCancellationTokenSource = new CancellationTokenSource();
                _ = FetchPools(poolCancellationTokenSource.Token);
            }
            else
            {
                OpenPoolToSelect();
            }
        }
        
        private async Task FetchPools(CancellationToken cancellationToken = default)
        {
            if (customListTableData == null)
            {
                return;
            }
            
            await poolLoadSemaphore.WaitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            
            try
            {
                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    customListTableData.tableView.ClearSelection();
                    Loaded = false;
                });

                // We check the cancellationtoken at each interval instead of running everything with a single token
                // due to unity not liking it
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var fetchedPools = await poolListSource.GetAsync(cancellationToken);

                if (fetchedPools == null || cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                switch (sortOption)
                {
                    case "Popularity":
                        fetchedPools.Sort(HitbloqPoolListEntry.PopularityComparer);
                        break;
                    case "Player Count":
                        fetchedPools.Sort(HitbloqPoolListEntry.PlayerCountComparer);
                        break;
                    case "Alphabetical":
                        fetchedPools.Sort(HitbloqPoolListEntry.NameComparer);
                        break;
                }

                if (sortDescending)
                {
                    fetchedPools.Reverse();
                }

                pools.Clear();
                pools.AddRange(fetchedPools);
            }
            finally
            {
                Loaded = true;
                await SiraUtil.Extras.Utilities.PauseChamp;
                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    customListTableData.tableView.ReloadData();
                });
                DetailDismissRequested?.Invoke();
                poolLoadSemaphore.Release();
                OpenPoolToSelect();
            }
        }

        private void OpenPoolToSelect()
        {
            if (poolToOpen != null && customListTableData != null)
            {
                for (var i = 0; i < pools.Count; i++)
                {
                    if (pools[i].ID == poolToOpen)
                    {
                        _ = IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                        {
                            customListTableData.tableView.SelectCellWithIdx(i);
                            customListTableData.tableView.ScrollToCellWithIdx(i, TableView.ScrollPositionType.Center, true);
                            PoolSelectedEvent?.Invoke(pools[i]);
                        });
                        break;
                    }
                }
                poolToOpen = null;
            }
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.localPosition = Vector3.zero;
            if (customListTableData != null)
            {
                customListTableData.tableView.SetDataSource(this, true);
            }
        }

        [UIAction("list-select")]
        private void OnListSelect(TableView _, int index) => PoolSelectedEvent?.Invoke(pools[index]);

        #region Sorting

        private string sortOption = "Popularity";
        private bool sortDescending = true;
        
        [UIAction("sort-selected")]
        private void SortSelected(string sortOption)
        {
            sortCancellationTokenSource?.Cancel();
            sortCancellationTokenSource?.Dispose();
            sortCancellationTokenSource = new CancellationTokenSource();
            this.sortOption = sortOption;
            _ = FetchPools(sortCancellationTokenSource.Token);
        }

        [UIAction("toggle-sort-direction")]
        private void ToggleSortDirection()
        {
            sortDescending = !sortDescending;
            NotifyPropertyChanged(nameof(SortDirection));
            
            sortCancellationTokenSource?.Cancel();
            sortCancellationTokenSource?.Dispose();
            sortCancellationTokenSource = new CancellationTokenSource();
            _ = FetchPools(sortCancellationTokenSource.Token);
        }
        
        [UIValue("sort-options")]
        private List<object> sortOptions = new() { "Popularity", "Player Count", "Alphabetical" };

        [UIValue("sort-direction")] 
        private string SortDirection => sortDescending ? "▼" : "▲";

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

        private const string kReuseIdentifier = "HitbloqPoolCell";
        
        private HitbloqPoolCellController GetCell()
        {
            var tableCell = customListTableData!.tableView.DequeueReusableCellForIdentifier(kReuseIdentifier);

            if (tableCell == null)
            {
                var hitbloqPoolCell = new GameObject(nameof(HitbloqPoolCellController), typeof(Touchable)).AddComponent<HitbloqPoolCellController>();
                hitbloqPoolCell.SetRequiredUtils(spriteLoader, materialGrabber, uwuTweenyManager);
                tableCell = hitbloqPoolCell;
                tableCell.interactable = true;

                tableCell.reuseIdentifier = kReuseIdentifier;
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqPoolCell.bsml"), tableCell.gameObject, tableCell);
            }

            return (HitbloqPoolCellController) tableCell;
        }

        public float CellSize() => 23;

        public int NumberOfCells() => pools.Count;

        public TableCell CellForIdx(TableView tableView, int idx) => GetCell().PopulateCell(pools[idx]);

        #endregion
    }
}
