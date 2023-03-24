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
		private readonly IconSegmentedControl _levelCategorySegmentedControl;
		private readonly LevelFilteringNavigationController _levelFilteringNavigationController;
		private readonly MainFlowCoordinator _mainFlowCoordinator;
		private readonly MainMenuViewController _mainMenuViewController;
		private readonly SelectLevelCategoryViewController _selectLevelCategoryViewController;
		private readonly IHttpService _siraHttpService;
		private readonly SoloFreePlayFlowCoordinator _soloFreePlayFlowCoordinator;

		private CancellationTokenSource? _tokenSource;

		public PlaylistManagerIHardlyKnowHer(IHttpService siraHttpService, MainFlowCoordinator mainFlowCoordinator, MainMenuViewController mainMenuViewController, SoloFreePlayFlowCoordinator soloFreePlayFlowCoordinator, LevelFilteringNavigationController levelFilteringNavigationController, SelectLevelCategoryViewController selectLevelCategoryViewController)
		{
			_siraHttpService = siraHttpService;
			_mainFlowCoordinator = mainFlowCoordinator;
			_mainMenuViewController = mainMenuViewController;
			_soloFreePlayFlowCoordinator = soloFreePlayFlowCoordinator;
			_levelFilteringNavigationController = levelFilteringNavigationController;
			_selectLevelCategoryViewController = selectLevelCategoryViewController;
			_levelCategorySegmentedControl = selectLevelCategoryViewController.GetField<IconSegmentedControl, SelectLevelCategoryViewController>("_levelFilterCategoryIconSegmentedControl");
		}

		public bool IsDownloading => _tokenSource is {IsCancellationRequested: false};

		public bool CanOpenPlaylist
		{
			get
			{
				var currentFlow = _mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
				var parentFlow = Accessors.ParentFlowAccessor(ref currentFlow);
				return parentFlow is LevelSelectionFlowCoordinator or MainFlowCoordinator;
			}
		}

		public void Dispose()
		{
			Events.playlistSelected -= OnPlaylistSelected;
		}

		public void Initialize()
		{
			Events.playlistSelected += OnPlaylistSelected;
		}

		public event Action<string>? HitbloqPlaylistSelected;

		public void DownloadOrOpenPlaylist(string poolID, Action? onDownloadComplete = null)
		{
			_ = DownloadOrOpenPlaylistAsync(poolID, onDownloadComplete);
		}

		private async Task DownloadOrOpenPlaylistAsync(string poolID, Action? onDownloadComplete = null)
		{
			_tokenSource?.Cancel();
			_tokenSource?.Dispose();
			_tokenSource = new CancellationTokenSource();
			var playlistToSelect = await GetPlaylist(poolID, _tokenSource.Token);

			if (playlistToSelect == null)
			{
				return;
			}

			await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
			{
				onDownloadComplete?.Invoke();
				OpenPlaylist(playlistToSelect);
			});

			_tokenSource.Dispose();
			_tokenSource = null;
		}

		public void CancelDownload()
		{
			_tokenSource?.Cancel();
		}

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
				var webResponse = await _siraHttpService.GetAsync(syncURL, cancellationToken: token).ConfigureAwait(false);
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
			if (_mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf() is LevelSelectionFlowCoordinator)
			{
				_levelCategorySegmentedControl.SelectCellWithNumber(1);
				_selectLevelCategoryViewController.LevelFilterCategoryIconSegmentedControlDidSelectCell(_levelCategorySegmentedControl, 1);
				_levelFilteringNavigationController.SelectAnnotatedBeatmapLevelCollection(playlist);
			}
			else
			{
				_soloFreePlayFlowCoordinator.Setup(Utils.GetStateForPlaylist(playlist));
				_mainMenuViewController.HandleMenuButton(MainMenuViewController.MenuButton.SoloFreePlay);
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