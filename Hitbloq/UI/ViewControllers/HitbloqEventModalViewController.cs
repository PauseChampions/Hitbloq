using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities;
using System.ComponentModel;
using System.Reflection;
using BeatSaberMarkupLanguage.Components;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI
{
    internal class HitbloqEventModalViewController : NotifiableBase, INotifyViewActivated
    {
        private readonly EventSource eventSource;
        private readonly SpriteLoader spriteLoader;
        private readonly PlaylistManagerIHardlyKnowHer? playlistManagerIHardlyKnowHer;

        private HitbloqEvent? currentEvent;
        private bool parsed;
        private bool downloadingActive;
        
        private Vector3? modalPosition;

        private bool DownloadingActive
        {
            get => downloadingActive;
            set
            {
                downloadingActive = value;
                NotifyPropertyChanged(nameof(PoolText));
            }
        }

        [UIComponent("modal")]
        private ModalView? modalView;

        [UIComponent("modal")]
        private readonly RectTransform? modalTransform = null!;

        [UIComponent("event-image")]
        private readonly ImageView? eventImage = null!;

        [UIComponent("text-page")]
        private readonly TextPageScrollView? descriptionTextPage = null!;

        [UIParams]
        private readonly BSMLParserParams? parserParams = null!;

        public HitbloqEventModalViewController(EventSource eventSource, SpriteLoader spriteLoader, [InjectOptional] PlaylistManagerIHardlyKnowHer playlistManagerIHardlyKnowHer)
        {
            this.eventSource = eventSource;
            this.spriteLoader = spriteLoader;
            this.playlistManagerIHardlyKnowHer = playlistManagerIHardlyKnowHer;
        }

        public async void ViewActivated(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                var hitbloqEvent = await eventSource.GetAsync();
                if (hitbloqEvent != null && hitbloqEvent.ID != -1)
                {
                    if (!PluginConfig.Instance.ViewedEvents.Contains(hitbloqEvent.ID))
                    {
                        ShowModal(hitbloqLeaderboardViewController.transform);
                        PluginConfig.Instance.ViewedEvents.Add(hitbloqEvent.ID);
                        PluginConfig.Instance.Changed();
                    }
                }
            }
        }

        private void Parse(Transform parentTransform)
        {
            if (!parsed)
            {
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqEventModal.bsml"), parentTransform.gameObject, this);
                modalPosition = modalTransform!.localPosition;
            }
            modalTransform!.SetParent(parentTransform);
            modalTransform.localPosition = modalPosition!.Value;
            Accessors.AnimateCanvasAccessor(ref modalView!) = true;
            descriptionTextPage!.ScrollTo(0, true);
        }

        internal void ShowModal(Transform parentTransform)
        {
            Parse(parentTransform);
            parserParams?.EmitEvent("close-modal");
            parserParams?.EmitEvent("open-modal");
        }

        [UIAction("#post-parse")]
        private async void PostParse()
        {
            parsed = true;
            modalView!.gameObject.name = "HitbloqEventModal";
            currentEvent = await eventSource.GetAsync();

            if (currentEvent?.Image != null)
            {
                _ = spriteLoader.DownloadSpriteAsync(currentEvent.Image, sprite => eventImage!.sprite = sprite);
            }

            NotifyPropertyChanged(nameof(EventTitle));
            NotifyPropertyChanged(nameof(EventDescription));
            NotifyPropertyChanged(nameof(PoolExists));
        }

        [UIAction("pool-click")]
        private void PoolClick()
        {
            if (PoolExists)
            {
                DownloadingActive = playlistManagerIHardlyKnowHer!.IsDownloading;
                if (DownloadingActive)
                {
                    playlistManagerIHardlyKnowHer.CancelDownload();
                }
                else
                {
                    playlistManagerIHardlyKnowHer.OpenPlaylist(currentEvent!.Pool!, () => DownloadingActive = false);
                }
                DownloadingActive = playlistManagerIHardlyKnowHer.IsDownloading;
            }
        }

        [UIValue("event-title")]
        private string EventTitle => $"{currentEvent?.Title}";

        [UIValue("event-description")]
        private string EventDescription => $"{currentEvent?.Description}";

        [UIValue("pool-text")]
        private string PoolText => DownloadingActive ? "Cancel Download" : "Open Pool!";

        [UIValue("pool-exists")]
        private bool PoolExists => playlistManagerIHardlyKnowHer != null && currentEvent != null && currentEvent.Pool != null;
    }
}
