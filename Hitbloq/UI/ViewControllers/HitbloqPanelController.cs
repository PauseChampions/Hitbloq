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
using UnityEngine;
using UnityEngine.EventSystems;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPanel.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPanel.bsml")]
    internal class HitbloqPanelController : BSMLAutomaticViewController, IInitializable, IDisposable, INotifyUserRegistered, IDifficultyBeatmapUpdater, IPoolUpdater, ILeaderboardEntriesUpdater
    {
        private MainFlowCoordinator mainFlowCoordinator;
        private HitbloqFlowCoordinator hitbloqFlowCoordinator;
        private PlaylistManagerIHardlyKnowHer playlistManagerIHardlyKnowHer;
        private RankInfoSource rankInfoSource;
        private PoolInfoSource poolInfoSource;
        private EventSource eventSource;

        private HitbloqRankInfo rankInfo;
        private List<string> poolNames;
        private string selectedPool;

        private bool _cuteMode;
        private string _promptText = "";
        private bool _loadingActive;
        private bool _downloadingActive;

        private Sprite logoSprite;
        private Sprite flushedSprite;
        private string spriteHoverText = "";

        private Color defaultHighlightColour;
        private Color cancelHighlightColor;

        private CancellationTokenSource poolInfoTokenSource;
        private CancellationTokenSource rankInfoTokenSource;

        public event Action<string> PoolChangedEvent;
        public event Action<HitbloqRankInfo, string> RankTextClickedEvent;
        public event Action LogoClickedEvent;

        private bool CuteMode
        {
            get => _cuteMode;
            set
            {
                if (_cuteMode != value)
                {
                    if (logo != null)
                    {
                        logo.sprite = value ? flushedSprite : logoSprite;
                        HoverHint hoverHint = logo.GetComponent<HoverHint>();
                        hoverHint.text = value ? "Pink Cute!" : spriteHoverText;
                        hoverHint.enabled = value || !string.IsNullOrEmpty(spriteHoverText);
                    }
                }
                _cuteMode = value;
            }
        }

        [UIComponent("container")]
        private readonly Backgroundable container;

        [UIComponent("hitbloq-logo")]
        private ImageView logo;

        [UIComponent("separator")]
        private ImageView separator;

        [UIComponent("dropdown-list")]
        private readonly DropDownListSetting dropDownListSetting;

        [UIComponent("dropdown-list")]
        private readonly RectTransform dropDownListTransform;

        [UIComponent("pm-image")]
        private readonly ClickableImage playlistManagerImage;

        [Inject]
        private void Inject(MainFlowCoordinator mainFlowCoordinator, HitbloqFlowCoordinator hitbloqFlowCoordinator, [InjectOptional] PlaylistManagerIHardlyKnowHer playlistManagerIHardlyKnowHer, 
            RankInfoSource rankInfoSource, PoolInfoSource poolInfoSource, EventSource eventSource)
        {
            this.mainFlowCoordinator = mainFlowCoordinator;
            this.hitbloqFlowCoordinator = hitbloqFlowCoordinator;
            this.playlistManagerIHardlyKnowHer = playlistManagerIHardlyKnowHer;
            this.rankInfoSource = rankInfoSource;
            this.poolInfoSource = poolInfoSource;
            this.eventSource = eventSource;
        }

        [UIAction("#post-parse")]
        private async void PostParse()
        {
            // Backround related stuff
            if (container.background is ImageView background)
            {
                background.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
                background.color0 = Color.white;
                background.color1 = new Color(1f, 1f, 1f, 0f);
                background.color = Color.gray;
                Accessors.GradientAccessor(ref background) = true;
                Accessors.SkewAccessor(ref background) = 0.18f;
            }

            // Loading up logos
            logoSprite = logo.sprite;
            flushedSprite = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.LogoFlushed.png");
            logo.sprite = CuteMode ? flushedSprite : logoSprite;
            logo.GetComponent<HoverHint>().enabled = CuteMode;

            Accessors.SkewAccessor(ref logo) = 0.18f;
            logo.SetVerticesDirty();

            Accessors.SkewAccessor(ref separator) = 0.18f;
            separator.SetVerticesDirty();

            // Dropdown needs to be modified to look good
            CurvedTextMeshPro dropdownText = dropDownListTransform.GetComponentInChildren<CurvedTextMeshPro>();
            dropdownText.fontSize = 3.5f;
            dropdownText.transform.localPosition = new Vector3(-1.5f, 0, 0);

            // A bit of explanation of what is going on
            // I want to make a maximum of 2 cells visible, however I first need to parse exactly 2 cells and clean them up
            // After that I populate the current pool options
            (dropDownListSetting.dropdown as DropdownWithTableView).SetField("_numberOfVisibleCells", 2);
            dropDownListSetting.values = new List<object>() { "1", "2" };
            dropDownListSetting.UpdateChoices();
            dropDownListSetting.values = pools.Count != 0 ? pools : new List<object> { "None" };
            dropDownListSetting.UpdateChoices();
            int poolIndex = poolNames == null ? 0 : poolNames.IndexOf(selectedPool);
            dropDownListSetting.dropdown.SelectCellWithIdx(poolIndex == -1 ? 0 : poolIndex);

            defaultHighlightColour = playlistManagerImage.HighlightColor;
            cancelHighlightColor = Color.red;

            HitbloqEvent hitbloqEvent = await eventSource.GetEventAsync();
            if (hitbloqEvent.id != -1)
            {
                ClickableImage clickableLogo = logo.Upgrade<ImageView, ClickableImage>();
                logo = clickableLogo;
                logoSprite = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.LogoEvent.png");
                logo.sprite = CuteMode ? flushedSprite : logoSprite;

                spriteHoverText = "Show event info";
                HoverHint hoverHint = logo.GetComponent<HoverHint>();
                hoverHint.text = CuteMode ? "Pink Cute!" : spriteHoverText;
                hoverHint.enabled = CuteMode || !string.IsNullOrEmpty(spriteHoverText);

                clickableLogo.OnClickEvent += LogoClicked;
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

            if (logo is ClickableImage clickableLogo)
            {
                clickableLogo.OnClickEvent -= LogoClicked;
            }
        }

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            NotifyPropertyChanged(nameof(PlaylistManagerActive));
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            dropDownListSetting.dropdown.Hide(false);
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        [UIAction("pool-changed")]
        private void PoolChanged(string formattedPool)
        {
            PoolChangedEvent?.Invoke(poolNames[dropDownListSetting.dropdown.selectedIndex]);
        }

        [UIAction("clicked-rank-text")]
        private void RankTextClicked()
        {
            RankTextClickedEvent?.Invoke(rankInfo, poolNames[dropDownListSetting.dropdown.selectedIndex]);
        }

        [UIAction("pm-click")]
        private void PlaylistManagerClicked()
        {
            if (PlaylistManagerActive)
            {
                DownloadingActive = playlistManagerIHardlyKnowHer.IsDownloading;
                if (DownloadingActive)
                {
                    playlistManagerIHardlyKnowHer.CancelDownload();
                }
                else
                {
                    playlistManagerIHardlyKnowHer.OpenPlaylist(selectedPool, () => DownloadingActive = false);
                }
                DownloadingActive = playlistManagerIHardlyKnowHer.IsDownloading;
            }
        }

        private void LogoClicked(PointerEventData pointerEventData) => LogoClickedEvent?.Invoke();

        private void OnPlaylistSelected(string pool) => selectedPool = pool;

        public void UserRegistered()
        {
            PromptText = "";
            LoadingActive = false;
        }

        public async void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo levelInfoEntry)
        {
            poolInfoTokenSource?.Cancel();
            poolInfoTokenSource = new CancellationTokenSource();

            pools = new List<object>();
            rankInfo = null;

            if (levelInfoEntry != null)
            {
                foreach(var pool in levelInfoEntry.pools)
                {
                    HitbloqPoolInfo poolInfo = await poolInfoSource.GetPoolInfoAsync(pool.Key, poolInfoTokenSource.Token);

                    string poolName = poolInfo.shownName.RemoveSpecialCharacters();
                    if (poolName.DoesNotHaveAlphaNumericCharacters())
                    {
                        poolName = poolInfo.id;
                    }
                    if (poolName.Length > 18)
                    {
                        poolName = $"{poolName.Substring(0, 15)}...";
                    }

                    pools.Add($"{poolName} - {pool.Value}⭐");
                }
                poolNames = levelInfoEntry.pools.Keys.ToList();
            }
            else
            {
                poolNames = new List<string> { "None" };
            }

            int poolIndex = poolNames.IndexOf(selectedPool);
            PoolChangedEvent?.Invoke(poolNames[poolIndex == -1 ? 0 : poolIndex]);

            if (dropDownListSetting != null)
            {
                dropDownListSetting.values = pools.Count != 0 ? pools : new List<object> { "None" };
                dropDownListSetting.UpdateChoices();
                dropDownListSetting.dropdown.SelectCellWithIdx(poolIndex == -1 ? 0 : poolIndex);

                if (!LoadingActive && !PromptText.Contains("<color=red>"))
                {
                    PromptText = "";
                }
            }
        }

        public async void PoolUpdated(string pool)
        {
            rankInfoTokenSource?.Cancel();
            rankInfoTokenSource = new CancellationTokenSource();
            selectedPool = pool;
            rankInfo = await rankInfoSource.GetRankInfoForSelfAsync(pool, rankInfoTokenSource.Token);
            NotifyPropertyChanged(nameof(PoolRankingText));
        }

        public void LeaderboardEntriesUpdated(List<HitbloqLeaderboardEntry> leaderboardEntries)
        {
            CuteMode = leaderboardEntries != null && leaderboardEntries.Exists(u => u.userID == 726);
        }

        [UIValue("prompt-text")]
        public string PromptText
        {
            get => _promptText;
            set
            {
                _promptText = value;
                NotifyPropertyChanged(nameof(PromptText));
            }
        }

        [UIValue("loading-active")]
        public bool LoadingActive
        {
            get => _loadingActive;
            set
            {
                _loadingActive = value;
                NotifyPropertyChanged(nameof(LoadingActive));
            }
        }

        [UIValue("downloading-active")]
        private bool DownloadingActive
        {
            get => _downloadingActive;
            set
            {
                _downloadingActive = value;
                playlistManagerImage.HighlightColor = value ? cancelHighlightColor : defaultHighlightColour;
                NotifyPropertyChanged(nameof(DownloadingActive));
                NotifyPropertyChanged(nameof(PlaylistManagerHoverHint));
            }
        }

        [UIValue("pool-ranking-text")]
        private string PoolRankingText => $"<b>Pool Ranking:</b> #{rankInfo?.rank} <size=75%>(<color=#aa6eff>{rankInfo?.cr.ToString("F2")}cr</color>)";

        [UIValue("pools")]
        private List<object> pools = new List<object> { "None" };

        [UIValue("pm-active")]
        private bool PlaylistManagerActive => playlistManagerIHardlyKnowHer != null && mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf() is SinglePlayerLevelSelectionFlowCoordinator;

        [UIValue("pm-hover")]
        private string PlaylistManagerHoverHint => DownloadingActive ? "Cancel playlist download" : "Open the playlist for this pool.";
    }
}