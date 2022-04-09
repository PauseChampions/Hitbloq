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

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            if (pools.Count == 0)
            {
                poolCancellationTokenSource?.Cancel();
                poolCancellationTokenSource?.Dispose();
                poolCancellationTokenSource = new CancellationTokenSource();
                _ = FetchPools(poolCancellationTokenSource.Token);
            }
        }

        private async Task FetchPools(CancellationToken cancellationToken)
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

                if (fetchedPools == null)
                {
                    return;
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
                poolLoadSemaphore.Release();
            }
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            if (customListTableData != null)
            {
                customListTableData.tableView.SetDataSource(this, true);
            }
        }
        
        [UIAction("list-select")]
        private void OnListSelect(TableView _, int index)
        {
        }

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

        public float CellSize() => 20;

        public int NumberOfCells() => pools.Count;

        public TableCell CellForIdx(TableView tableView, int idx) => GetCell().PopulateCell(pools[idx]);

        #endregion
    }
}
