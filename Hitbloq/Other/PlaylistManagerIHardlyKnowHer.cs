using HMUI;
using IPA.Utilities;
using SiraUtil;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Hitbloq.Other
{
    internal class PlaylistManagerIHardlyKnowHer
    {
        private readonly SiraClient siraClient;
        private readonly LevelFilteringNavigationController levelFilteringNavigationController;
        private readonly SelectLevelCategoryViewController selectLevelCategoryViewController;
        private readonly IconSegmentedControl levelCategorySegmentedControl;

        private CancellationTokenSource tokenSource;

        public PlaylistManagerIHardlyKnowHer(SiraClient siraClient, LevelFilteringNavigationController levelFilteringNavigationController, SelectLevelCategoryViewController selectLevelCategoryViewController)
        {
            this.siraClient = siraClient;
            this.levelFilteringNavigationController = levelFilteringNavigationController;
            this.selectLevelCategoryViewController = selectLevelCategoryViewController;
            levelCategorySegmentedControl = selectLevelCategoryViewController.GetField<IconSegmentedControl, SelectLevelCategoryViewController>("_levelFilterCategoryIconSegmentedControl");
        }

        internal async void OpenPlaylist(string poolID, Action onDownloadComplete = null)
        {
            tokenSource?.Dispose();
            tokenSource = new CancellationTokenSource();
            IBeatmapLevelPack playlistToSelect = await GetPlaylist(poolID);

            if (playlistToSelect == null)
            {
                return;
            }

            onDownloadComplete?.Invoke();
            levelCategorySegmentedControl.SelectCellWithNumber(1);
            selectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(levelCategorySegmentedControl, 1);
            levelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(playlistToSelect);
        }

        internal void CancelDownload() => tokenSource?.Cancel();

        private async Task<IBeatmapLevelPack> GetPlaylist(string poolID)
        {
            string syncURL = $"https://hitbloq.com/static/hashlists/{poolID}.bplist";

            List<BeatSaberPlaylistsLib.Types.IPlaylist> playlists = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.GetAllPlaylists(true).ToList();
            foreach (var playlist in playlists)
            {
                if (playlist.TryGetCustomData("syncURL", out object url) && url is string urlString)
                {
                    if (urlString == syncURL)
                    {
                        return playlist;
                    }
                }
            }

            try
            {
                WebResponse webResponse = await siraClient.GetAsync(syncURL, tokenSource.Token).ConfigureAwait(false);
                Stream playlistStream = new MemoryStream(webResponse.ContentToBytes());
                BeatSaberPlaylistsLib.Types.IPlaylist newPlaylist = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.DefaultHandler?.Deserialize(playlistStream);

                BeatSaberPlaylistsLib.PlaylistManager playlistManager = BeatSaberPlaylistsLib.PlaylistManager.DefaultManager.CreateChildManager("Hitbloq");
                playlistManager.StorePlaylist(newPlaylist);
                return newPlaylist;
            }
            catch (TaskCanceledException)
            {
                return null;
            }
        }
    }
}
