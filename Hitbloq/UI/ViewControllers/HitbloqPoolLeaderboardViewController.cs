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
using IPA.Utilities.Async;
using Tweening;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\HitbloqPoolLeaderboardView.bsml")]
	[ViewDefinition("Hitbloq.UI.Views.HitbloqPoolLeaderboardView.bsml")]
	internal class HitbloqPoolLeaderboardViewController : BSMLAutomaticViewController, TableView.IDataSource
	{
		[UIComponent("list")]
		private readonly CustomListTableData? _customListTableData = null!;
		
		[UIComponent("vertical-icon-segments")]

		private readonly IconSegmentedControl? _iconSegmentedControl = null!;

		private readonly List<HitbloqPoolLeaderboardEntry> _leaderboardEntries = new();

		private readonly SemaphoreSlim _leaderboardLoadSemaphore = new(1, 1);

		[Inject]
		private readonly List<IPoolLeaderboardSource> _leaderboardSources = null!;

		[Inject]
		private readonly MaterialGrabber _materialGrabber = null!;

		[Inject]
		private readonly HitbloqProfileModalController _profileModalController = null!;

		[Inject]
		private readonly SpriteLoader _spriteLoader = null!;

		[Inject]
		private readonly UserIDSource _userIDSource = null!;

		[Inject]
		private readonly TimeTweeningManager _uwuTweenyManager = null!;

		private CancellationTokenSource? _cancellationTokenSource;
		private PoolLeaderboardPage? _currentPage;
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

			_ = InitLeaderboard(poolID, _cancellationTokenSource.Token, true);
		}

		private void OnScrollPositionChanged(float newPos)
		{
			if (_scrollView == null || _currentPage == null || _currentPage.ExhaustedPages || _currentScrollPosition == newPos || _leaderboardLoadSemaphore.CurrentCount == 0)
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
				_ = InitLeaderboard("", _cancellationTokenSource.Token, false);
			}
		}

		[UIAction("#post-parse")]
		private async Task PostParse()
		{
			var list = new List<IconSegmentedControl.DataItem>();
			foreach (var leaderboardSource in _leaderboardSources)
			{
				list.Add(new IconSegmentedControl.DataItem(await leaderboardSource.Icon, leaderboardSource.HoverHint));
			}

			_iconSegmentedControl!.SetData(list.ToArray());
			
			if (_customListTableData != null)
			{
				_customListTableData.TableView.SetDataSource(this, true);
				_scrollView = _customListTableData.TableView.scrollView;
			}
		}

		[UIAction("list-select")]
		private void Select(TableView tableView, int idx)
		{
			tableView.ClearSelection();
			if (_currentPage != null)
			{
				_profileModalController.ShowModalForUser(transform, _leaderboardEntries[idx].UserID, _currentPage.PoolID);
			}
		}

		private async Task InitLeaderboard(string poolID, CancellationToken cancellationToken, bool firstPage)
		{
			if (_customListTableData == null)
			{
				return;
			}

			await _leaderboardLoadSemaphore.WaitAsync(cancellationToken);
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
						_leaderboardEntries.Clear();
						_customListTableData.TableView.ClearSelection();
						_customListTableData.TableView.ReloadData();
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

				if (firstPage || _currentPage == null)
				{
					page = await _leaderboardSources[SelectedCellIndex].GetScoresAsync(poolID, cancellationToken);
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
				_leaderboardEntries.AddRange(_currentPage.Entries);
			}
			finally
			{
				Loaded = true;
				await SiraUtil.Extras.Utilities.PauseChamp;
				await UnityMainThreadTaskScheduler.Factory.StartNew(() => { _customListTableData.TableView.ReloadDataKeepingPosition(); });
				_leaderboardLoadSemaphore.Release();

				if (firstPage && _currentPage is {ExhaustedPages: false})
				{
					// Request another page if on first
					_ = InitLeaderboard("", cancellationToken, false);
				}
			}
		}

		#region Segmented Control

		private int _selectedCellIndex;

		private int SelectedCellIndex
		{
			get => _selectedCellIndex;
			set
			{
				_selectedCellIndex = value;
				if (_currentPage != null)
				{
					_cancellationTokenSource?.Cancel();
					_cancellationTokenSource?.Dispose();
					_cancellationTokenSource = new CancellationTokenSource();
					_ = InitLeaderboard(_currentPage.PoolID, _cancellationTokenSource.Token, true);
				}
			}
		}

		[UIAction("cell-selected")]
		private void OnCellSelected(SegmentedControl _, int index)
		{
			SelectedCellIndex = index;
		}

		#endregion

		#region Loading

		private bool _loaded;

		[UIValue("is-loading")]
		public bool IsLoading => !Loaded;

		[UIValue("has-loaded")]
		public bool Loaded
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

		#region TableData

		private const string KReuseIdentifier = "HitbloqPoolLeaderboardCell";

		private HitbloqPoolLeaderboardCellController GetCell()
		{
			var tableCell = _customListTableData!.TableView.DequeueReusableCellForIdentifier(KReuseIdentifier);

			if (tableCell == null)
			{
				var hitbloqPoolLeaderboardCell = new GameObject(nameof(HitbloqPoolLeaderboardCellController), typeof(Touchable)).AddComponent<HitbloqPoolLeaderboardCellController>();
				hitbloqPoolLeaderboardCell.SetRequiredUtils(_userIDSource, _spriteLoader, _materialGrabber, _uwuTweenyManager);
				tableCell = hitbloqPoolLeaderboardCell;
				tableCell.interactable = true;

				tableCell.reuseIdentifier = KReuseIdentifier;
				BSMLParser.Instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqPoolLeaderboardCell.bsml"), tableCell.gameObject, tableCell);
			}

			return (HitbloqPoolLeaderboardCellController) tableCell;
		}

		public float CellSize(int idx)
		{
			return 7;
		}

		public int NumberOfCells()
		{
			return _leaderboardEntries.Count;
		}

		public TableCell CellForIdx(TableView tableView, int idx)
		{
			return GetCell().PopulateCell(_leaderboardEntries[idx]);
		}

		#endregion
	}
}