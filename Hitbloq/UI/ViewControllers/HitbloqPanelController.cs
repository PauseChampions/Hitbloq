using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities;
using IPA.Utilities.Async;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\HitbloqPanel.bsml")]
	[ViewDefinition("Hitbloq.UI.Views.HitbloqPanel.bsml")]
	internal class HitbloqPanelController : BSMLAutomaticViewController, IInitializable, IDisposable, INotifyUserRegistered, IDifficultyBeatmapUpdater, IPoolUpdater, ILeaderboardEntriesUpdater
	{
		private readonly Color _cancelHighlightColor = Color.red;

		[UIComponent("container")]
		private readonly Backgroundable? _container = null!;

		[UIComponent("dropdown-list")]
		private readonly DropDownListSetting? _dropDownListSetting = null!;

		[UIComponent("dropdown-list")]
		private readonly RectTransform? _dropDownListTransform = null!;

		[Inject]
		private readonly IEventSource _eventSource = null!;

		[Inject]
		private readonly MainFlowCoordinator _mainFlowCoordinator = null!;

		[InjectOptional]
		private readonly PlaylistManagerIHardlyKnowHer? _playlistManagerIHardlyKnowHer = null!;

		[UIComponent("pm-image")]
		private readonly ClickableImage? _playlistManagerImage = null!;

		[Inject]
		private readonly PoolInfoSource _poolInfoSource = null!;

		[Inject]
		private readonly RankInfoSource _rankInfoSource = null!;

		private bool _cuteMode;

		private Color? _defaultHighlightColour;
		private bool _downloadingActive;
		private bool _eventActive;

		[UIComponent("event-image")]
		private ImageView? _eventImage;

		private Sprite? _flushedSprite;
		private bool _loadingActive;

		[UIComponent("hitbloq-logo")]
		private ImageView? _logo;

		private Sprite? _logoSprite;

		private CancellationTokenSource? _poolInfoTokenSource;
		private List<string>? _poolNames;

		[UIValue("pools")] private List<object> _pools = new() {"None"};
		private string _promptText = "";

		private HitbloqRankInfo? _rankInfo;
		private CancellationTokenSource? _rankInfoTokenSource;
		private string? _selectedPool;

		[UIComponent("separator")]
		private ImageView? _separator;

		private bool CuteMode
		{
			get => _cuteMode;
			set
			{
				if (_cuteMode != value)
				{
					if (_logo != null)
					{
						_logo.sprite = value ? _flushedSprite : _logoSprite;
						var hoverHint = _logo.GetComponent<HoverHint>();
						hoverHint.text = value ? "Pink Cute!" : "Open Hitbloq Menu";
					}
				}

				_cuteMode = value;
			}
		}

		[UIValue("prompt-text")]
		public string PromptText
		{
			get => _promptText;
			set
			{
				_promptText = value;
				NotifyPropertyChanged();
			}
		}

		[UIValue("loading-active")]
		public bool LoadingActive
		{
			get => _loadingActive;
			set
			{
				_loadingActive = value;
				NotifyPropertyChanged();
			}
		}

		[UIValue("downloading-active")]
		private bool DownloadingActive
		{
			get => _downloadingActive;
			set
			{
				_downloadingActive = value;

				if (_playlistManagerImage != null && _defaultHighlightColour != null)
				{
					_playlistManagerImage.HighlightColor = value ? _cancelHighlightColor : _defaultHighlightColour.Value;
				}

				NotifyPropertyChanged();
				NotifyPropertyChanged(nameof(PlaylistManagerHoverHint));
			}
		}

		[UIValue("pool-ranking-text")]
		private string PoolRankingText =>
			$"<b>Pool Ranking:</b> #{_rankInfo?.Rank} <size=75%>(<color=#aa6eff>{_rankInfo?.CR.ToString("F2")}cr</color>)";

		[UIValue("pm-active")]
		private bool PlaylistManagerActive => _playlistManagerIHardlyKnowHer != null && _mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf() is SinglePlayerLevelSelectionFlowCoordinator;

		[UIValue("pm-hover")]
		private string PlaylistManagerHoverHint =>
			DownloadingActive ? "Cancel playlist download" : "Open the playlist for this pool.";

		[UIValue("event-active")]
		private bool EventActive
		{
			get => _eventActive;
			set
			{
				_eventActive = value;
				NotifyPropertyChanged();
			}
		}

		public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo? levelInfoEntry)
		{
			_ = DifficultyBeatmapUpdatedAsync(levelInfoEntry);
		}

		public void Dispose()
		{
			if (_playlistManagerIHardlyKnowHer != null)
			{
				_playlistManagerIHardlyKnowHer.HitbloqPlaylistSelected -= OnPlaylistSelected;
			}
		}

		public void Initialize()
		{
			if (_playlistManagerIHardlyKnowHer != null)
			{
				_playlistManagerIHardlyKnowHer.HitbloqPlaylistSelected += OnPlaylistSelected;
			}
		}

		public void LeaderboardEntriesUpdated(List<HitbloqMapLeaderboardEntry>? leaderboardEntries)
		{		
			CuteMode = leaderboardEntries != null && leaderboardEntries.Exists(u => u.UserID == 726);
		}

		public void UserRegistered()
		{
			PromptText = "";
			LoadingActive = false;
		}

		public void PoolUpdated(string pool)
		{
			_ = PoolUpdatedAsync(pool);
		}

		public event Action<string>? PoolChangedEvent;
		public event Action<HitbloqRankInfo, string>? RankTextClickedEvent;
		public event Action? LogoClickedEvent;
		public event Action? EventClickedEvent;

		[UIAction("#post-parse")]
		private async Task PostParse()
		{
			// Background related stuff
			if (_container!.background is ImageView background)
			{
				background.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
				background.color0 = Color.white;
				background.color1 = new Color(1f, 1f, 1f, 0f);
				background.color = Color.gray;
				Accessors.GradientAccessor(ref background) = true;
				Accessors.SkewAccessor(ref background) = 0.18f;
			}

			// Loading up logos
			_logoSprite = await BeatSaberMarkupLanguage.Utilities.LoadSpriteFromAssemblyAsync("Hitbloq.Images.Logo.png");
			_flushedSprite = await BeatSaberMarkupLanguage.Utilities.LoadSpriteFromAssemblyAsync("Hitbloq.Images.LogoFlushed.png");
			_logo!.sprite = CuteMode ? _flushedSprite : _logoSprite;

			Accessors.SkewAccessor(ref _logo) = 0.18f;
			_logo.SetVerticesDirty();

			Accessors.SkewAccessor(ref _separator!) = 0.18f;
			_separator.SetVerticesDirty();

			// Dropdown needs to be modified to look good
			var dropdownText = _dropDownListTransform!.GetComponentInChildren<CurvedTextMeshPro>();
			dropdownText.fontSize = 3.5f;
			dropdownText.transform.localPosition = new Vector3(-1.5f, 0, 0);

			// A bit of explanation of what is going on
			// I want to make a maximum of 2 cells visible, however I first need to parse exactly 2 cells and clean them up
			// After that I populate the current pool options
			(_dropDownListSetting!.dropdown as DropdownWithTableView).SetField("_numberOfVisibleCells", 2);
			_dropDownListSetting.values = new List<object> {"1", "2"};
			_dropDownListSetting.UpdateChoices();
			_dropDownListSetting.values = _pools.Count != 0 ? _pools : new List<object> {"None"};
			_dropDownListSetting.UpdateChoices();
			var poolIndex = _poolNames?.IndexOf(_selectedPool ?? "") ?? 0;
			_dropDownListSetting.dropdown.SelectCellWithIdx(poolIndex == -1 ? 0 : poolIndex);

			_defaultHighlightColour = _playlistManagerImage!.HighlightColor;

			_ = FetchEvent();
		}

		private async Task FetchEvent()
		{
			var hitbloqEvent = await _eventSource.GetAsync();
			if (hitbloqEvent != null && hitbloqEvent.ID != -1)
			{
				EventActive = true;
				Accessors.SkewAccessor(ref _eventImage!) = 0.18f;
			}
		}

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
			NotifyPropertyChanged(nameof(PlaylistManagerActive));
		}

		protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			if (_dropDownListSetting != null)
			{
				_dropDownListSetting.dropdown.Hide(false);
			}

			base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
		}

		[UIAction("pool-changed")]
		private void PoolChanged(string formattedPool)
		{
			if (_dropDownListSetting != null && _poolNames != null)
			{
				PoolChangedEvent?.Invoke(_poolNames[_dropDownListSetting.dropdown.selectedIndex]);
			}
		}

		[UIAction("clicked-rank-text")]
		private void RankTextClicked()
		{
			if (_dropDownListSetting != null && _rankInfo != null && _poolNames != null)
			{
				RankTextClickedEvent?.Invoke(_rankInfo, _poolNames[_dropDownListSetting.dropdown.selectedIndex]);
			}
		}

		[UIAction("pm-click")]
		private void PlaylistManagerClicked()
		{
			if (PlaylistManagerActive && _selectedPool != null)
			{
				DownloadingActive = _playlistManagerIHardlyKnowHer!.IsDownloading;
				if (DownloadingActive)
				{
					_playlistManagerIHardlyKnowHer.CancelDownload();
				}
				else
				{
					_playlistManagerIHardlyKnowHer.DownloadOrOpenPlaylist(_selectedPool, () => DownloadingActive = false);
				}

				DownloadingActive = _playlistManagerIHardlyKnowHer.IsDownloading;
			}
		}

		[UIAction("logo-click")]
		private void LogoClicked()
		{
			LogoClickedEvent?.Invoke();
		}

		[UIAction("event-click")]
		private void EventClicked()
		{
			EventClickedEvent?.Invoke();
		}

		private void OnPlaylistSelected(string pool)
		{
			_selectedPool = pool;
		}

		private async Task DifficultyBeatmapUpdatedAsync(HitbloqLevelInfo? levelInfoEntry)
		{
			_poolInfoTokenSource?.Cancel();
			_poolInfoTokenSource?.Dispose();
			_poolInfoTokenSource = new CancellationTokenSource();

			_pools = new List<object>();
			_rankInfo = null;

			if (levelInfoEntry != null)
			{
				foreach (var pool in levelInfoEntry.Pools)
				{
					var poolInfo = await _poolInfoSource.GetPoolInfoAsync(pool.Key, _poolInfoTokenSource.Token);

					var poolName = poolInfo?.ShownName.RemoveSpecialCharacters() ?? pool.Key;
					if (poolName.DoesNotHaveAlphaNumericCharacters())
					{
						poolName = pool.Key;
					}

					if (poolName.Length > 18)
					{
						poolName = $"{poolName.Substring(0, 15)}...";
					}

					_pools.Add($"{poolName} - {pool.Value}⭐");
				}

				_poolNames = levelInfoEntry.Pools.Keys.ToList();
			}
			else
			{
				_poolNames = new List<string> {"None"};
			}

			int poolIndex;

			if (PluginConfig.Instance.PrioritisePlaylistPool && _playlistManagerIHardlyKnowHer is {SelectedPlaylist: not null})
			{
				poolIndex = _poolNames.IndexOf(PlaylistManagerIHardlyKnowHer.GetPlaylistPool(_playlistManagerIHardlyKnowHer.SelectedPlaylist) ?? "");
			}
			else
			{
				poolIndex = _poolNames.IndexOf(_selectedPool ?? "");
			}
			
			await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
			{
				PoolChangedEvent?.Invoke(_poolNames[poolIndex == -1 ? 0 : poolIndex]);

				if (_dropDownListSetting != null)
				{
					_dropDownListSetting.values = _pools.Count != 0 ? _pools : new List<object> {"None"};
					_dropDownListSetting.UpdateChoices();
					_dropDownListSetting.dropdown.SelectCellWithIdx(poolIndex == -1 ? 0 : poolIndex);

					if (!LoadingActive && !PromptText.Contains("<color=red>"))
					{
						PromptText = "";
					}
				}
			});
		}

		private async Task PoolUpdatedAsync(string pool)
		{
            _rankInfoTokenSource?.Cancel();
			_rankInfoTokenSource?.Dispose();
			_rankInfoTokenSource = new CancellationTokenSource();
			_selectedPool = pool;
			_rankInfo = await _rankInfoSource.GetRankInfoForSelfAsync(pool, _rankInfoTokenSource.Token);
			NotifyPropertyChanged(nameof(PoolRankingText));
		}
	}
}