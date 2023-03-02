using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberPlaylistsLib.Types;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities;
using IPA.Utilities.Async;
using PlaylistManager.Utilities;
using SiraUtil.Web;
using Zenject;
using Utils = Hitbloq.Utilities.Utils;

namespace Hitbloq.Other
{
    internal class PlaylistManagerIHardlyKnowHer : IInitializable, IDisposable
    {
        private readonly IHttpService siraHttpService;
        private readonly MainFlowCoordinator mainFlowCoordinator;
        private readonly MainMenuViewController mainMenuViewController;
        private readonly SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator;
        private readonly LevelFilteringNavigationController levelFilteringNavigationController;
        private readonly SelectLevelCategoryViewController selectLevelCategoryViewController;
        private readonly IconSegmentedControl levelCategorySegmentedControl;

        private CancellationTokenSource? tokenSource;

        public bool IsDownloading => tokenSource is {IsCancellationRequested: false};

        public bool CanOpenPlaylist
        {
            get
            {
                var currentFlow = mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
                var parentFlow = Accessors.ParentFlowAccessor(ref currentFlow);
                return parentFlow is LevelSelectionFlowCoordinator or MainFlowCoordinator;
            }
        } 
        public event Action<string>? HitbloqPlaylistSelected;

        public PlaylistManagerIHardlyKnowHer(IHttpService siraHttpService, MainFlowCoordinator mainFlowCoordinator, MainMenuViewController mainMenuViewController, SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator, LevelFilteringNavigationController levelFilteringNavigationController, SelectLevelCategoryViewController selectLevelCategoryViewController)
        {
            this.siraHttpService = siraHttpService;
            this.mainFlowCoordinator = mainFlowCoordinator;
            this.mainMenuViewController = mainMenuViewController;
            this.soloFreePlayFlowCoordinator = soloFreePlayFlowCoordinator;
            this.levelFilteringNavigationController = levelFilteringNavigationController;
            this.selectLevelCategoryViewController = selectLevelCategoryViewController;
            levelCategorySegmentedControl = selectLevelCategoryViewController.GetField<IconSegmentedControl, SelectLevelCategoryViewController>("_levelFilterCategoryIconSegmentedControl");
        }

        public void Initialize()
        {
            Events.playlistSelected += OnPlaylistSelected;
        }

        public void Dispose()
        {
            Events.playlistSelected -= OnPlaylistSelected;
        }

        public void DownloadOrOpenPlaylist(string poolID, Action? onDownloadComplete = null)
            => _ = DownloadOrOpenPlaylistAsync(poolID, onDownloadComplete);

        private async Task DownloadOrOpenPlaylistAsync(string poolID, Action? onDownloadComplete = null)
        {
            tokenSource?.Cancel();
            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource();
            var playlistToSelect = await GetPlaylist(poolID, tokenSource.Token);

            if (playlistToSelect == null)
            {
                return;
            }

            await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                onDownloadComplete?.Invoke();
                OpenPlaylist(playlistToSelect);
            });

            tokenSource.Dispose();
            tokenSource = null;
        }

        public void CancelDownload() => tokenSource?.Cancel();
        
        private async Task<IBeatmapLevelPack?> GetPlaylist(string poolID, CancellationToken token = default)
        {
            var localPlaylist = await FindLocalPlaylistFromPoolID(poolID, token);
            if (localPlaylist != null)
            {
                return localPlaylist;
            }

            return await DownloadPlaylistFromPoolID(poolID, token).ConfigureAwait(false);
        }

        public async Task<IBeatmapLevelPack?> FindLocalPlaylistFromPoolID(string poolID, CancellationToken token = default)
        {
            try
            {
                return await Task.Run(() =>
                {
                    var syncURL = $"https://hitbloq.com/static/hashlists/{poolID}.bplist";

                    var playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true).ToList();
                    foreach (var playlist in playlists)
                    {
                        if (playlist.TryGetCustomData("syncURL", out var url) && url is string urlString)
                        {
                            if (urlString == syncURL)
                            {
                                return playlist;
                            }
                        }
                    }

                    return null;
                }, token).ConfigureAwait(false);   
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        public async Task<IBeatmapLevelPack?> DownloadPlaylistFromPoolID(string poolID, CancellationToken token = default)
        {
            try
            {
                var syncURL = $"https://hitbloq.com/static/hashlists/{poolID}.bplist";
                var webResponse = await siraHttpService.GetAsync(syncURL, cancellationToken: token).ConfigureAwait(false);
                Stream playlistStream = new MemoryStream(await webResponse.ReadAsByteArrayAsync());
                var newPlaylist = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.DefaultHandler?.Deserialize(playlistStream);

                if (newPlaylist != null)
                {
                    var playlistManager = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.CreateChildManager("Hitbloq");
                    playlistManager.StorePlaylist(newPlaylist);   
                }
                
                return newPlaylist;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }

        public void OpenPlaylist(IBeatmapLevelPack playlist)
        {
            if (mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf() is LevelSelectionFlowCoordinator)
            {
                levelCategorySegmentedControl.SelectCellWithNumber(1);
                selectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(levelCategorySegmentedControl, 1);
                levelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(playlist);
            }
            else
            {
                soloFreePlayFlowCoordinator.Setup(Utils.GetStateForPlaylist(playlist));
                mainMenuViewController.HandleMenuButton(MainMenuViewController.MenuButton.SoloFreePlay);
            }
        }

        private void OnPlaylistSelected(IPlaylist playlist, BeatSaberPlaylistsLib.PlaylistManager parentManager)
        {
            if (playlist.TryGetCustomData("syncURL", out var url) && url is string urlString)
            {
                if (urlString.Contains("https://hitbloq.com/static/hashlists/"))
                {
                    var pool = urlString.Split('/').LastOrDefault()?.Split('.').FirstOrDefault();
                    if (!string.IsNullOrEmpty(pool))
                    {
                        HitbloqPlaylistSelected?.Invoke(pool!);
                    }
                }
            }
        }
    }
}
