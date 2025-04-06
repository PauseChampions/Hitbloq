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
using IPA.Utilities.Async;
using Tweening;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\HitbloqPoolListView.bsml")]
	[ViewDefinition("Hitbloq.UI.Views.HitbloqPoolListView.bsml")]
	internal class HitbloqPoolListViewController : BSMLAutomaticViewController, TableView.IDataSource
	{
		public string? poolToOpen;

		[UIComponent("list")]
		private readonly CustomListTableData? _customListTableData = null!;

		[Inject]
		private readonly MaterialGrabber _materialGrabber = null!;

		[Inject]
		private readonly PoolListSource _poolListSource = null!;

		private readonly SemaphoreSlim _poolLoadSemaphore = new(1, 1);

		private readonly List<HitbloqPoolListEntry> _pools = new();

		[Inject]
		private readonly SpriteLoader _spriteLoader = null!;

		[Inject]
		private readonly TimeTweeningManager _uwuTweenyManager = null!;

		private CancellationTokenSource? _poolCancellationTokenSource;
		private CancellationTokenSource? _sortCancellationTokenSource;

		public event Action<HitbloqPoolListEntry>? PoolSelectedEvent;
		public event Action? DetailDismissRequested;

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);

			if (_customListTableData != null)
			{
				_customListTableData.TableView.ClearSelection();
			}

			if (_pools.Count == 0)
			{
				_poolCancellationTokenSource?.Cancel();
				_poolCancellationTokenSource?.Dispose();
				_poolCancellationTokenSource = new CancellationTokenSource();
				_ = FetchPools(_poolCancellationTokenSource.Token);
			}
			else
			{
				OpenPoolToSelect();
			}
		}

		private async Task FetchPools(CancellationToken cancellationToken = default)
		{
			if (_customListTableData == null)
			{
				return;
			}

			await _poolLoadSemaphore.WaitAsync(cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			try
			{
				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					_customListTableData.TableView.ClearSelection();
					Loaded = false;
				});

				// We check the cancellationtoken at each interval instead of running everything with a single token
				// due to unity not liking it
				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				var fetchedPools = await _poolListSource.GetAsync(cancellationToken);

				if (fetchedPools == null || cancellationToken.IsCancellationRequested)
				{
					return;
				}

				switch (_sortOption)
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

				if (_sortDescending)
				{
					fetchedPools.Reverse();
				}

				_pools.Clear();
				_pools.AddRange(fetchedPools);
			}
			finally
			{
				Loaded = true;
				await SiraUtil.Extras.Utilities.PauseChamp;
				await UnityMainThreadTaskScheduler.Factory.StartNew(() => { _customListTableData.TableView.ReloadData(); });
				DetailDismissRequested?.Invoke();
				_poolLoadSemaphore.Release();
				OpenPoolToSelect();
			}
		}

		private void OpenPoolToSelect()
		{
			if (poolToOpen != null && _customListTableData != null)
			{
				for (var i = 0; i < _pools.Count; i++)
				{
					if (_pools[i].ID == poolToOpen)
					{
						_ = UnityMainThreadTaskScheduler.Factory.StartNew(() =>
						{
							_customListTableData.TableView.SelectCellWithIdx(i);
							_customListTableData.TableView.ScrollToCellWithIdx(i, TableView.ScrollPositionType.Center, true);
							PoolSelectedEvent?.Invoke(_pools[i]);
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
			if (_customListTableData != null)
			{
				_customListTableData.TableView.SetDataSource(this, true);
			}
		}

		[UIAction("list-select")]
		private void OnListSelect(TableView _, int index)
		{
			PoolSelectedEvent?.Invoke(_pools[index]);
		}

		#region Sorting

		private string _sortOption = "Popularity";
		private bool _sortDescending = true;

		[UIAction("sort-selected")]
		private void SortSelected(string sortOption)
		{
			_sortCancellationTokenSource?.Cancel();
			_sortCancellationTokenSource?.Dispose();
			_sortCancellationTokenSource = new CancellationTokenSource();
			_sortOption = sortOption;
			_ = FetchPools(_sortCancellationTokenSource.Token);
		}

		[UIAction("toggle-sort-direction")]
		private void ToggleSortDirection()
		{
			_sortDescending = !_sortDescending;
			NotifyPropertyChanged(nameof(SortDirection));

			_sortCancellationTokenSource?.Cancel();
			_sortCancellationTokenSource?.Dispose();
			_sortCancellationTokenSource = new CancellationTokenSource();
			_ = FetchPools(_sortCancellationTokenSource.Token);
		}

		[UIValue("sort-options")]
		private List<object> _sortOptions = new() {"Popularity", "Player Count", "Alphabetical"};

		[UIValue("sort-direction")]
		private string SortDirection => _sortDescending ? "▼" : "▲";

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

		private const string KReuseIdentifier = "HitbloqPoolCell";

		private HitbloqPoolCellController GetCell()
		{
			var tableCell = _customListTableData!.TableView.DequeueReusableCellForIdentifier(KReuseIdentifier);

			if (tableCell == null)
			{
				var hitbloqPoolCell = new GameObject(nameof(HitbloqPoolCellController), typeof(Touchable)).AddComponent<HitbloqPoolCellController>();
				hitbloqPoolCell.SetRequiredUtils(_spriteLoader, _materialGrabber, _uwuTweenyManager);
				tableCell = hitbloqPoolCell;
				tableCell.interactable = true;

				tableCell.reuseIdentifier = KReuseIdentifier;
				BSMLParser.Instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqPoolCell.bsml"), tableCell.gameObject, tableCell);
			}

			return (HitbloqPoolCellController) tableCell;
		}

		public float CellSize(int idx)
		{
			return 23;
		}

		public int NumberOfCells()
		{
			return _pools.Count;
		}

		public TableCell CellForIdx(TableView tableView, int idx)
		{
			return GetCell().PopulateCell(_pools[idx]);
		}

		#endregion
	}
}