using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using HMUI;
using System.Threading;
using System.Threading.Tasks;
using Hitbloq.Other;
using Hitbloq.Pages;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqRankedListView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqRankedListView.bsml")]
    internal class HitbloqRankedListViewController : BSMLAutomaticViewController
    {
        [Inject] 
        private readonly RankedListDetailedSource rankedListDetailedSource = null!;
        
        [Inject]
        private readonly SpriteLoader spriteLoader = null!;

        private RankedListDetailedPage? currentPage;
        private CancellationTokenSource? cancellationTokenSource;
        
        private ScrollView? scrollView;
        private float? currentScrollPosition;
        
        private readonly SemaphoreSlim songLoadSemaphore = new(1, 1);
        
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

            _ = InitSongList(poolID, cancellationTokenSource.Token, true);
        }

        private void OnScrollPositionChanged(float newPos)
        {
            if (scrollView == null || currentPage == null || currentPage.ExhaustedPages || currentScrollPosition == newPos || songLoadSemaphore.CurrentCount == 0)
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
                _ = InitSongList("", cancellationTokenSource.Token, false);
            }
        }
        
        [UIAction("#post-parse")]
        private void PostParse() => scrollView = Accessors.ScrollViewAccessor(ref customListTableData!.tableView);

        [UIAction("list-select")]
        private void Select(TableView _, int __) => customListTableData!.tableView.ClearSelection();

        private async Task InitSongList(string poolID, CancellationToken cancellationToken, bool firstPage)
        {
            if (customListTableData == null)
            {
                return;
            }

            await songLoadSemaphore.WaitAsync(cancellationToken);
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
                        customListTableData.data.Clear();
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

                RankedListDetailedPage? page;

                if (firstPage || currentPage == null)
                {
                    page = await rankedListDetailedSource.GetRankedListAsync(poolID, cancellationToken, 0);
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
            
                foreach (var song in page.Entries)
                {
                    var customCellInfo = new CustomListTableData.CustomCellInfo(song.Name, $"{song.Difficulty} [{song.Stars}⭐]",
                        BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);
                
                    _ = spriteLoader.DownloadSpriteAsync(song.CoverURL, sprite =>
                    {
                        customCellInfo.icon = sprite;
                        customListTableData.tableView.ReloadDataKeepingPosition();
                    }, cancellationToken);
                
                    customListTableData.data.Add(customCellInfo);
                                
                    if (cancellationToken.IsCancellationRequested)
                    {
                        return;
                    }
                }
            }
            finally
            {
                Loaded = true;
                await SiraUtil.Extras.Utilities.PauseChamp;
                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    customListTableData.tableView.ReloadDataKeepingPosition();
                });
                songLoadSemaphore.Release();
                
                if (firstPage)
                {
                    // Request another page if on first
                    _ = InitSongList("", cancellationToken, false);
                }
            }
        }
        
        #region Loading

        private bool loaded;
        [UIValue("is-loading")]
        private bool IsLoading => !Loaded;

        [UIValue("has-loaded")]
        private bool Loaded
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
    }
}
