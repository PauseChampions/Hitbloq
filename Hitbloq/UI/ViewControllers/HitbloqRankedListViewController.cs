using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Other;
using Hitbloq.Pages;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities.Async;
using Zenject;

namespace Hitbloq.UI
{
	[HotReload(RelativePathToLayout = @"..\Views\HitbloqRankedListView.bsml")]
	[ViewDefinition("Hitbloq.UI.Views.HitbloqRankedListView.bsml")]
	internal class HitbloqRankedListViewController : BSMLAutomaticViewController
	{
		[UIComponent("list")]
		private readonly CustomListTableData? _customListTableData = null!;

		[Inject]
		private readonly RankedListDetailedSource _rankedListDetailedSource = null!;

		private readonly SemaphoreSlim _songLoadSemaphore = new(1, 1);

		[Inject]
		private readonly SpriteLoader _spriteLoader = null!;

		private CancellationTokenSource? _cancellationTokenSource;

		private RankedListDetailedPage? _currentPage;
		private float? _currentScrollPosition;

		private ScrollView? _scrollView;

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
			if (_scrollView != null)
			{
				_scrollView.scrollPositionChangedEvent += OnScrollPositionChanged;
			}
		}

		protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
			if (_scrollView != null)
			{
				_scrollView.scrollPositionChangedEvent -= OnScrollPositionChanged;
			}
		}

		public void SetPool(string poolID)
		{
			if (_customListTableData == null)
			{
				return;
			}

			_cancellationTokenSource?.Cancel();
			_cancellationTokenSource?.Dispose();
			_cancellationTokenSource = new CancellationTokenSource();

			_ = InitSongList(poolID, _cancellationTokenSource.Token, true);
		}

		private void OnScrollPositionChanged(float newPos)
		{
			if (_scrollView == null || _currentPage == null || _currentPage.ExhaustedPages || _currentScrollPosition == newPos || _songLoadSemaphore.CurrentCount == 0)
			{
				return;
			}

			_currentScrollPosition = newPos;
			_scrollView.RefreshButtons();

			if (!Accessors.PageDownAccessor(ref _scrollView).interactable)
			{
				_cancellationTokenSource?.Cancel();
				_cancellationTokenSource?.Dispose();
				_cancellationTokenSource = new CancellationTokenSource();
				_ = InitSongList("", _cancellationTokenSource.Token, false);
			}
		}

		[UIAction("#post-parse")]
		private void PostParse()
		{
			_scrollView = Accessors.ScrollViewAccessor(ref _customListTableData!.tableView);
		}

		[UIAction("list-select")]
		private void Select(TableView _, int __)
		{
			_customListTableData!.tableView.ClearSelection();
		}

		private async Task InitSongList(string poolID, CancellationToken cancellationToken, bool firstPage)
		{
			if (_customListTableData == null)
			{
				return;
			}

			await _songLoadSemaphore.WaitAsync(cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			try
			{
				if (firstPage)
				{
					await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
					{
						_customListTableData.data.Clear();
						_customListTableData.tableView.ReloadData();
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

				if (firstPage || _currentPage == null)
				{
					page = await _rankedListDetailedSource.GetRankedListAsync(poolID, cancellationToken);
				}
				else
				{
					page = await _currentPage.Next(cancellationToken);
				}

				if (cancellationToken.IsCancellationRequested || page == null)
				{
					return;
				}

				_currentPage = page;

				foreach (var song in page.Entries)
				{
					var customCellInfo = new CustomListTableData.CustomCellInfo(song.Name, $"{song.Difficulty} [{song.Stars}⭐]", BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite);

					_ = _spriteLoader.DownloadSpriteAsync(song.CoverURL, sprite =>
					{
						customCellInfo.icon = sprite;
						_customListTableData.tableView.ReloadDataKeepingPosition();
					}, cancellationToken);

					_customListTableData.data.Add(customCellInfo);

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
				await UnityMainThreadTaskScheduler.Factory.StartNew(() => { _customListTableData.tableView.ReloadDataKeepingPosition(); });
				_songLoadSemaphore.Release();

				if (firstPage)
				{
					// Request another page if on first
					_ = InitSongList("", cancellationToken, false);
				}
			}
		}

		#region Loading

		private bool _loaded;

		[UIValue("is-loading")]
		private bool IsLoading => !Loaded;

		[UIValue("has-loaded")]
		private bool Loaded
		{
			get => _loaded;
			set
			{
				_loaded = value;
				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(IsLoading));
			}
		}

		#endregion
	}
}