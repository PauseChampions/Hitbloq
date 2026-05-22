using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities.Async;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\HitbloqLeaderboardView.bsml")]
	[ViewDefinition("Hitbloq.UI.Views.HitbloqLeaderboardView.bsml")]
	internal class HitbloqLeaderboardViewController : BSMLAutomaticViewController, IBeatmapKeyUpdater, ILeaderboardEntriesUpdater, IPoolUpdater
	{
		[UIComponent("vertical-icon-segments")]
		private readonly IconSegmentedControl? _iconSegmentedControl = null!;
		
		[UIComponent("leaderboard")]
		private readonly LeaderboardTableView? _leaderboard = null!;

		[Inject]
		private readonly List<IMapLeaderboardSource> _leaderboardSources = null!;

		[UIComponent("leaderboard")]
		private readonly Transform? _leaderboardTransform = null!;

		[Inject]
		private readonly HitbloqProfileModalController _profileModalController = null!;

		[UIValue("cell-clicker-holders")]
		private readonly List<HitbloqLeaderboardCellClickingView> _cellClickingHolders = Enumerable.Range(0, 10).Select(_ => new HitbloqLeaderboardCellClickingView()).ToList();

		[Inject]
		private readonly MaterialGrabber _materialGrabber = null!;

		[UIValue("profile-image-holders")]
		private readonly List<HitbloqLeaderboardProfilePictureView> _profileImageHolders = Enumerable.Range(0, 10).Select(_ => new HitbloqLeaderboardProfilePictureView()).ToList();

		[Inject]
		private readonly ProfileSource _profileSource = null!;

		[Inject]
		private readonly SpriteLoader _spriteLoader = null!;

		[Inject]
		private readonly UserIDSource _userIDSource = null!;

		private BeatmapKey? _beatmapKey;

		private List<HitbloqMapLeaderboardEntry>? _leaderboardEntries;
		private bool _mapRankedOnAnyPool;

		private LoadingControl? _loadingControl;

		private int _pageNumber;
		private int _renderVersion;
		private CancellationTokenSource? _profilePictureTokenSource;
		private string? _selectedPool;
		private Vector2? _scoreSaberPlayerNamePosition;

		private int PageNumber
		{
			get => _pageNumber;
			set
			{
				_pageNumber = value;
				NotifyPropertyChanged(nameof(UpEnabled));
				if (_leaderboard != null && _loadingControl != null && _beatmapKey != null && Utils.IsDependencyLeaderboardInstalled)
				{
					_leaderboard.SetScores(new List<LeaderboardTableView.ScoreData>(), 0);
					_loadingControl.ShowLoading();
					PageRequested?.Invoke(_beatmapKey.Value, _leaderboardSources[SelectedCellIndex], value);
				}
			}
		}

		[UIValue("up-enabled")]
		private bool UpEnabled => PageNumber != 0 && _leaderboardSources[SelectedCellIndex].Scrollable;

		[UIValue("down-enabled")]
		private bool DownEnabled => _leaderboardEntries is {Count: 10} && _leaderboardSources[SelectedCellIndex].Scrollable;

		public void BeatmapKeyUpdated(BeatmapKey beatmapKey, HitbloqLevelInfo? levelInfoEntry)
		{
			_beatmapKey = beatmapKey;
			_mapRankedOnAnyPool = levelInfoEntry != null;

			if (isActiveAndEnabled)
			{
				foreach (var leaderboardSource in _leaderboardSources)
				{
					leaderboardSource.ClearCache();
				}

				if (_mapRankedOnAnyPool)
				{
					PageNumber = 0;
				}
				else
				{
					_leaderboardEntries = null;
					_ = SetScores(null);
				}
			}
		}

		public void LeaderboardEntriesUpdated(List<HitbloqMapLeaderboardEntry>? leaderboardEntries)
		{
			_leaderboardEntries = leaderboardEntries;
			NotifyPropertyChanged(nameof(DownEnabled));
			_ = SetScores(leaderboardEntries);
		}

		public void PoolUpdated(string pool)
		{
			_selectedPool = pool;
			if (isActiveAndEnabled)
			{
				_ = SetScores(_leaderboardEntries);
			}
		}

		public event Action<BeatmapKey, IMapLeaderboardSource, int>? PageRequested;

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
			foreach (var leaderboardSource in _leaderboardSources)
			{
				leaderboardSource.ClearCache();
			}

			if (!firstActivation && !Utils.IsDependencyLeaderboardInstalled)
			{
				SetNoDependenciesInstalledText();
			}

			PageNumber = 0;
		}

		private async Task SetScores(List<HitbloqMapLeaderboardEntry>? leaderboardEntries)
		{
			if (Utils.IsDependencyLeaderboardInstalled is false)
				return;

			var renderVersion = ++_renderVersion;
			var scores = new List<LeaderboardTableView.ScoreData>();
			var myScorePos = -1;

			_profilePictureTokenSource?.Cancel();
			_profilePictureTokenSource?.Dispose();
			_profilePictureTokenSource = new CancellationTokenSource();

			await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
			{
				foreach (var profileImageHolder in _profileImageHolders)
				{
					profileImageHolder.ClearSprite();
				}

				foreach (var cellClickingHolder in _cellClickingHolders)
				{
					cellClickingHolder.ClearClicker();
				}
			});

			if (leaderboardEntries == null || leaderboardEntries.Count == 0)
			{
				scores.Add(new LeaderboardTableView.ScoreData(0, _mapRankedOnAnyPool ? "You haven't set a score on this leaderboard - <size=75%>(<color=#FFD42A>0%</color>)</size>" : "<color=red>This map is not ranked on any pools</color>", 0, false));
			}
			else
			{
				if (_selectedPool == null || !leaderboardEntries.First().CR.ContainsKey(_selectedPool))
				{
					scores.Add(new LeaderboardTableView.ScoreData(0, "<color=red>This map is not ranked on any pools</color>", 0, false));
				}
				else
				{
					var userID = await _userIDSource.GetUserIDAsync();
					var id = userID?.ID ?? -1;

					await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
					{
						for (var i = 0; i < (leaderboardEntries.Count > 10 ? 10 : leaderboardEntries.Count); i++)
						{
							scores.Add(new LeaderboardTableView.ScoreData(leaderboardEntries[i].Score, $"<color={leaderboardEntries[i].CustomColor ?? "#ffffff"}><size=85%>{leaderboardEntries[i].Username}</color> - <size=75%>(<color=#FFD42A>{leaderboardEntries[i].Accuracy.ToString("F2")}%</color>)</size></size> - <size=75%> (<color=#aa6eff>{leaderboardEntries[i].CR[_selectedPool].ToString("F2")}<size=55%>cr</size></color>)</size>", leaderboardEntries[i].Rank, false));

							if (leaderboardEntries[i].UserID == id)
							{
								myScorePos = i;
							}
						}
					});

					_ = SetProfilePictures(leaderboardEntries, renderVersion, _profilePictureTokenSource.Token);
				}
			}

			if (renderVersion != _renderVersion)
			{
				return;
			}

			if (_loadingControl != null && _leaderboard != null)
			{
				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					if (renderVersion != _renderVersion)
					{
						return;
					}

					_loadingControl.Hide();
					_leaderboard.SetScores(scores, myScorePos);
					var leaderboardTableCells = _leaderboardTransform!.GetComponentsInChildren<LeaderboardTableCell>(true);
					for (var i = 0; i < leaderboardTableCells.Length; i++)
					{
						ApplyScoreSaberCellStyle(leaderboardTableCells[i]);

						if (i < scores.Count && i < _cellClickingHolders.Count && leaderboardEntries != null && i < leaderboardEntries.Count)
						{
							var separatorIndex = Math.Min(i + 1, leaderboardTableCells.Length - 1);
							var leaderboardTableCell = leaderboardTableCells[separatorIndex];
							var separator = Accessors.LeaderboardCellSeparatorAccessor(ref leaderboardTableCell);
							_cellClickingHolders[i].SetClicker(i, InfoButtonClicked, separator);
						}
					}
				});
			}
		}

		private async Task SetProfilePictures(IReadOnlyList<HitbloqMapLeaderboardEntry> leaderboardEntries, int renderVersion, CancellationToken cancellationToken)
		{
			try
			{
				var count = Math.Min(leaderboardEntries.Count, _profileImageHolders.Count);
				var profileTasks = Enumerable.Range(0, count)
					.Select(index => FetchProfilePictureAsync(leaderboardEntries[index].UserID, cancellationToken))
					.ToArray();

				var profilePictures = await Task.WhenAll(profileTasks);

				if (renderVersion != _renderVersion)
				{
					return;
				}

				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					if (renderVersion != _renderVersion)
					{
						return;
					}

					for (var i = 0; i < profilePictures.Length; i++)
					{
						if (cancellationToken.IsCancellationRequested)
						{
							return;
						}

						if (profilePictures[i] != null)
						{
							_profileImageHolders[i].SetProfilePicture(profilePictures[i]!, cancellationToken);
						}
						else
						{
							_profileImageHolders[i].ClearSprite();
						}
					}
				});
			}
			catch (TaskCanceledException)
			{
			}
		}

		private async Task<string?> FetchProfilePictureAsync(int userID, CancellationToken cancellationToken)
		{
			try
			{
				var profile = await _profileSource.GetProfileAsync(userID, cancellationToken);
				return cancellationToken.IsCancellationRequested ? null : profile?.ProfilePictureURL;
			}
			catch (Exception)
			{
				return null;
			}
		}


		public void InfoButtonClicked(int index)
		{
			if (_leaderboardEntries != null && index < _leaderboardEntries.Count && _selectedPool != null)
			{
				_profileModalController.ShowModalForUser(transform, _leaderboardEntries[index].UserID, _selectedPool);
			}
		}

		private void SetNoDependenciesInstalledText()
		{
			if (_loadingControl is not null)
			{
				_loadingControl.ShowText("<size=125%>Please install ScoreSaber or BeatLeader!</size>", false);
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
			
			// To set rich text, I have to iterate through all cells, set each cell to allow rich text and next time they will have it
			var leaderboardTableCells = _leaderboardTransform!.GetComponentsInChildren<LeaderboardTableCell>(true);

			foreach (var leaderboardTableCell in leaderboardTableCells)
			{
				ApplyScoreSaberCellStyle(leaderboardTableCell);
			}

			foreach (var profileImageHolder in _profileImageHolders)
			{
				profileImageHolder.SetRequiredUtils(_spriteLoader, _materialGrabber);
			}

			_loadingControl = _leaderboardTransform.GetComponentInChildren<LoadingControl>(true);

			var loadingContainer = _loadingControl.transform.Find("LoadingContainer");
			loadingContainer.gameObject.SetActive(true);
			_loadingControl.ShowLoading();

			if (Utils.IsDependencyLeaderboardInstalled is false)
			{
				SetNoDependenciesInstalledText();
			}
		}

		private void ApplyScoreSaberCellStyle(LeaderboardTableCell leaderboardTableCell)
		{
			leaderboardTableCell._playerNameText.richText = true;
			leaderboardTableCell.showSeparator = true;

			if (_scoreSaberPlayerNamePosition == null)
			{
				_scoreSaberPlayerNamePosition = leaderboardTableCell._playerNameText.rectTransform.anchoredPosition;
			}

			leaderboardTableCell._playerNameText.rectTransform.anchoredPosition = new Vector2(_scoreSaberPlayerNamePosition.Value.x + 2.5f, 0f);
		}

		[UIAction("up-clicked")]
		private void UpClicked()
		{
			if (UpEnabled)
			{
				PageNumber--;
			}
		}

		[UIAction("down-clicked")]
		private void DownClicked()
		{
			if (DownEnabled)
			{
				PageNumber++;
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
				PageNumber = 0;
			}
		}

		[UIAction("cell-selected")]
		private void OnCellSelected(SegmentedControl _, int index)
		{
			SelectedCellIndex = index;
		}

		#endregion
	}
}
