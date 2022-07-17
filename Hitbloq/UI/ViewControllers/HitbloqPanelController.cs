using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPanel.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPanel.bsml")]
    internal class HitbloqPanelController : BSMLAutomaticViewController, IInitializable, IDisposable,
        INotifyUserRegistered, IDifficultyBeatmapUpdater, IPoolUpdater, ILeaderboardEntriesUpdater
    {
        [Inject]
        private readonly MainFlowCoordinator mainFlowCoordinator = null!;

        [InjectOptional] 
        private readonly PlaylistManagerIHardlyKnowHer? playlistManagerIHardlyKnowHer = null!;

        [Inject] 
        private readonly RankInfoSource rankInfoSource = null!;

        [Inject] 
        private readonly PoolInfoSource poolInfoSource = null!;

        [Inject]
        private readonly IEventSource eventSource = null!;

        private HitbloqRankInfo? rankInfo;
        private List<string>? poolNames;
        private string? selectedPool;

        private bool cuteMode;
        private string promptText = "";
        private bool loadingActive;
        private bool downloadingActive;
        private bool eventActive;

        private Sprite? logoSprite;
        private Sprite? flushedSprite;
        private string spriteHoverText = "";

        private Color? defaultHighlightColour;
        private readonly Color cancelHighlightColor = Color.red;

        private CancellationTokenSource? poolInfoTokenSource;
        private CancellationTokenSource? rankInfoTokenSource;

        public event Action<string>? PoolChangedEvent;
        public event Action<HitbloqRankInfo, string>? RankTextClickedEvent;
        public event Action? LogoClickedEvent;
        public event Action? EventClickedEvent;

        private bool CuteMode
        {
            get => cuteMode;
            set
            {
                if (cuteMode != value)
                {
                    if (logo != null)
                    {
                        logo.sprite = value ? flushedSprite : logoSprite;
                        var hoverHint = logo.GetComponent<HoverHint>();
                        hoverHint.text = value ? "Pink Cute!" : spriteHoverText;
                        hoverHint.enabled = value || !string.IsNullOrEmpty(spriteHoverText);
                    }
                }

                cuteMode = value;
            }
        }

        [UIComponent("container")] 
        private readonly Backgroundable? container = null!;

        [UIComponent("hitbloq-logo")] 
        private ImageView? logo;

        [UIComponent("separator")] 
        private ImageView? separator;

        [UIComponent("event-image")]
        private ImageView? eventImage;

        [UIComponent("dropdown-list")]
        private readonly DropDownListSetting? dropDownListSetting = null!;

        [UIComponent("dropdown-list")]
        private readonly RectTransform? dropDownListTransform = null!;

        [UIComponent("pm-image")] 
        private readonly ClickableImage? playlistManagerImage = null!;

        [UIAction("#post-parse")]
        private void PostParse()
        {
            // Backround related stuff
            if (container!.background is ImageView background)
            {
                background.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
                background.color0 = Color.white;
                background.color1 = new Color(1f, 1f, 1f, 0f);
                background.color = Color.gray;
                Accessors.GradientAccessor(ref background) = true;
                Accessors.SkewAccessor(ref background) = 0.18f;
            }

            // Loading up logos
            logoSprite = logo!.sprite;
            flushedSprite = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.LogoFlushed.png");
            logo.sprite = CuteMode ? flushedSprite : logoSprite;
            logo.GetComponent<HoverHint>().enabled = CuteMode;

            Accessors.SkewAccessor(ref logo) = 0.18f;
            logo.SetVerticesDirty();

            Accessors.SkewAccessor(ref separator!) = 0.18f;
            separator.SetVerticesDirty();

            // Dropdown needs to be modified to look good
            var dropdownText = dropDownListTransform!.GetComponentInChildren<CurvedTextMeshPro>();
            dropdownText.fontSize = 3.5f;
            dropdownText.transform.localPosition = new Vector3(-1.5f, 0, 0);

            // A bit of explanation of what is going on
            // I want to make a maximum of 2 cells visible, however I first need to parse exactly 2 cells and clean them up
            // After that I populate the current pool options
            (dropDownListSetting!.dropdown as DropdownWithTableView).SetField("_numberOfVisibleCells", 2);
            dropDownListSetting.values = new List<object>() {"1", "2"};
            dropDownListSetting.UpdateChoices();
            dropDownListSetting.values = pools.Count != 0 ? pools : new List<object> {"None"};
            dropDownListSetting.UpdateChoices();
            var poolIndex = poolNames?.IndexOf(selectedPool ?? "") ?? 0;
            dropDownListSetting.dropdown.SelectCellWithIdx(poolIndex == -1 ? 0 : poolIndex);

            defaultHighlightColour = playlistManagerImage!.HighlightColor;

            _ = FetchEvent();
        }

        private async Task FetchEvent()
        {
            var hitbloqEvent = await eventSource.GetAsync();
            if (hitbloqEvent != null && hitbloqEvent.ID != -1)
            {
                EventActive = true;
                Accessors.SkewAccessor(ref eventImage!) = 0.18f;
            }
        }

        public void Initialize()
        {
            if (playlistManagerIHardlyKnowHer != null)
            {
                playlistManagerIHardlyKnowHer.HitbloqPlaylistSelected += OnPlaylistSelected;
            }
        }

        public void Dispose()
        {
            if (playlistManagerIHardlyKnowHer != null)
            {
                playlistManagerIHardlyKnowHer.HitbloqPlaylistSelected -= OnPlaylistSelected;
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            NotifyPropertyChanged(nameof(PlaylistManagerActive));
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            if (dropDownListSetting != null)
            {
                dropDownListSetting.dropdown.Hide(false);
            }

            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        [UIAction("pool-changed")]
        private void PoolChanged(string formattedPool)
        {
            if (dropDownListSetting != null && poolNames != null)
            {
                PoolChangedEvent?.Invoke(poolNames[dropDownListSetting.dropdown.selectedIndex]);
            }
        }

        [UIAction("clicked-rank-text")]
        private void RankTextClicked()
        {
            if (dropDownListSetting != null && rankInfo != null && poolNames != null)
            {
                RankTextClickedEvent?.Invoke(rankInfo, poolNames[dropDownListSetting.dropdown.selectedIndex]);
            }
        }

        [UIAction("pm-click")]
        private void PlaylistManagerClicked()
        {
            if (PlaylistManagerActive && selectedPool != null)
            {
                DownloadingActive = playlistManagerIHardlyKnowHer!.IsDownloading;
                if (DownloadingActive)
                {
                    playlistManagerIHardlyKnowHer.CancelDownload();
                }
                else
                {
                    playlistManagerIHardlyKnowHer.DownloadOrOpenPlaylist(selectedPool, () => DownloadingActive = false);
                }

                DownloadingActive = playlistManagerIHardlyKnowHer.IsDownloading;
            }
        }
        
        [UIAction("event-click")]
        private void EventClicked() => EventClickedEvent?.Invoke();

        private void LogoClicked(PointerEventData pointerEventData) => LogoClickedEvent?.Invoke();

        private void OnPlaylistSelected(string pool) => selectedPool = pool;

        public void UserRegistered()
        {
            PromptText = "";
            LoadingActive = false;
        }

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo? levelInfoEntry) =>
            _ = DifficultyBeatmapUpdatedAsync(levelInfoEntry);

        private async Task DifficultyBeatmapUpdatedAsync(HitbloqLevelInfo? levelInfoEntry)
        {
            poolInfoTokenSource?.Cancel();
            poolInfoTokenSource?.Dispose();
            poolInfoTokenSource = new CancellationTokenSource();

            pools = new List<object>();
            rankInfo = null;

            if (levelInfoEntry != null)
            {
                foreach (var pool in levelInfoEntry.Pools)
                {
                    var poolInfo = await poolInfoSource.GetPoolInfoAsync(pool.Key, poolInfoTokenSource.Token);

                    var poolName = poolInfo?.ShownName.RemoveSpecialCharacters() ?? pool.Key;
                    if (poolName.DoesNotHaveAlphaNumericCharacters())
                    {
                        poolName = pool.Key;
                    }

                    if (poolName.Length > 18)
                    {
                        poolName = $"{poolName.Substring(0, 15)}...";
                    }

                    pools.Add($"{poolName} - {pool.Value}⭐");
                }

                poolNames = levelInfoEntry.Pools.Keys.ToList();
            }
            else
            {
                poolNames = new List<string> {"None"};
            }

            var poolIndex = poolNames.IndexOf(selectedPool ?? "");

            await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                PoolChangedEvent?.Invoke(poolNames[poolIndex == -1 ? 0 : poolIndex]);

                if (dropDownListSetting != null)
                {
                    dropDownListSetting.values = pools.Count != 0 ? pools : new List<object> {"None"};
                    dropDownListSetting.UpdateChoices();
                    dropDownListSetting.dropdown.SelectCellWithIdx(poolIndex == -1 ? 0 : poolIndex);

                    if (!LoadingActive && !PromptText.Contains("<color=red>"))
                    {
                        PromptText = "";
                    }
                }
            });
        }

        public void PoolUpdated(string pool) => _ = PoolUpdatedAsync(pool);

        private async Task PoolUpdatedAsync(string pool)
        {
            rankInfoTokenSource?.Cancel();
            rankInfoTokenSource?.Dispose();
            rankInfoTokenSource = new CancellationTokenSource();
            selectedPool = pool;
            rankInfo = await rankInfoSource.GetRankInfoForSelfAsync(pool, rankInfoTokenSource.Token);
            NotifyPropertyChanged(nameof(PoolRankingText));
        }

        public void LeaderboardEntriesUpdated(List<HitbloqMapLeaderboardEntry>? leaderboardEntries)
        {
            CuteMode = leaderboardEntries != null && leaderboardEntries.Exists(u => u.UserID == 726);
        }

        [UIValue("prompt-text")]
        public string PromptText
        {
            get => promptText;
            set
            {
                promptText = value;
                NotifyPropertyChanged(nameof(PromptText));
            }
        }

        [UIValue("loading-active")]
        public bool LoadingActive
        {
            get => loadingActive;
            set
            {
                loadingActive = value;
                NotifyPropertyChanged(nameof(LoadingActive));
            }
        }

        [UIValue("downloading-active")]
        private bool DownloadingActive
        {
            get => downloadingActive;
            set
            {
                downloadingActive = value;

                if (playlistManagerImage != null && defaultHighlightColour != null)
                {
                    playlistManagerImage.HighlightColor = value ? cancelHighlightColor : defaultHighlightColour.Value;
                }

                NotifyPropertyChanged(nameof(DownloadingActive));
                NotifyPropertyChanged(nameof(PlaylistManagerHoverHint));
            }
        }

        [UIValue("pool-ranking-text")]
        private string PoolRankingText =>
            $"<b>Pool Ranking:</b> #{rankInfo?.Rank} <size=75%>(<color=#aa6eff>{rankInfo?.CR.ToString("F2")}cr</color>)";

        [UIValue("pools")] private List<object> pools = new() {"None"};

        [UIValue("pm-active")]
        private bool PlaylistManagerActive => playlistManagerIHardlyKnowHer != null &&
                                              mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf() is
                                                  SinglePlayerLevelSelectionFlowCoordinator;

        [UIValue("pm-hover")]
        private string PlaylistManagerHoverHint =>
            DownloadingActive ? "Cancel playlist download" : "Open the playlist for this pool.";

        [UIValue("event-active")]
        private bool EventActive
        {
            get => eventActive;
            set
            {
                eventActive = value;
                NotifyPropertyChanged();
            }
        }
    }
}