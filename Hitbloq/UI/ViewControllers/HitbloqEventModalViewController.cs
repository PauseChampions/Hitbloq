using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Sources;
using HMUI;
using IPA.Utilities;
using System.ComponentModel;
using System.Reflection;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI
{
    internal class HitbloqEventModalViewController : INotifyViewActivated, INotifyPropertyChanged
    {
        private readonly EventSource eventSource;
        private readonly SpriteLoader spriteLoader;
        private readonly IVRPlatformHelper platformHelper;
        private readonly PlaylistManagerIHardlyKnowHer playlistManagerIHardlyKnowHer;

        private HitbloqEvent currentEvent;
        private bool parsed;
        private bool _downloadingActive;

        public event PropertyChangedEventHandler PropertyChanged;

        private Vector3 modalPosition;

        private bool DownloadingActive
        {
            get => _downloadingActive;
            set
            {
                _downloadingActive = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PoolText)));
            }
        }

        [UIComponent("modal")]
        private readonly ModalView modalView;

        [UIComponent("modal")]
        private readonly RectTransform modalTransform;

        [UIComponent("event-image")]
        private readonly ImageView eventImage;

        [UIComponent("text-page")]
        private readonly TextPageScrollView descriptionTextPage;

        [UIParams]
        private readonly BSMLParserParams parserParams;

        public HitbloqEventModalViewController(EventSource eventSource, SpriteLoader spriteLoader, IVRPlatformHelper platformHelper,
            [InjectOptional] PlaylistManagerIHardlyKnowHer playlistManagerIHardlyKnowHer)
        {
            this.eventSource = eventSource;
            this.spriteLoader = spriteLoader;
            this.platformHelper = platformHelper;
            this.playlistManagerIHardlyKnowHer = playlistManagerIHardlyKnowHer;
        }

        public async void ViewActivated(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            if (firstActivation)
            {
                HitbloqEvent hitbloqEvent = await eventSource.GetEventAsync();
                if (hitbloqEvent != null && hitbloqEvent.id != -1)
                {
                    if (!PluginConfig.Instance.ViewedEvents.Contains(hitbloqEvent.id))
                    {
                        ShowModal(hitbloqLeaderboardViewController.transform);
                        PluginConfig.Instance.ViewedEvents.Add(hitbloqEvent.id);
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
                modalPosition = modalTransform.localPosition;
            }
            modalTransform.SetParent(parentTransform);
            modalTransform.localPosition = modalPosition;
            modalView.SetField("_animateParentCanvas", true);
            descriptionTextPage.ScrollTo(0, true);
        }

        internal void ShowModal(Transform parentTransform)
        {
            Parse(parentTransform);
            parserParams.EmitEvent("close-modal");
            parserParams.EmitEvent("open-modal");
        }

        [UIAction("#post-parse")]
        private async void PostParse()
        {
            parsed = true;
            modalView.gameObject.name = "HitbloqEventModal";
            (descriptionTextPage as ScrollView).SetField("_platformHelper", platformHelper);

            currentEvent = await eventSource.GetEventAsync();

            if (currentEvent != null && currentEvent.image != null)
            {
                spriteLoader.DownloadSpriteAsync(currentEvent.image, (Sprite sprite) => eventImage.sprite = sprite);
            }

            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EventTitle)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(EventDescription)));
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PoolExists)));
        }

        [UIAction("pool-click")]
        private void PoolClick()
        {
            if (PoolExists)
            {
                DownloadingActive = playlistManagerIHardlyKnowHer.IsDownloading;
                if (DownloadingActive)
                {
                    playlistManagerIHardlyKnowHer.CancelDownload();
                }
                else
                {
                    playlistManagerIHardlyKnowHer.OpenPlaylist(currentEvent.pool, () => DownloadingActive = false);
                }
                DownloadingActive = playlistManagerIHardlyKnowHer.IsDownloading;
            }
        }

        [UIValue("event-title")]
        private string EventTitle => $"{currentEvent?.title}";

        [UIValue("event-description")]
        private string EventDescription => $"{currentEvent?.description}";

        [UIValue("pool-text")]
        private string PoolText => DownloadingActive ? "Cancel Download" : "Open Pool!";

        [UIValue("pool-exists")]
        private bool PoolExists => playlistManagerIHardlyKnowHer != null && currentEvent != null && currentEvent.pool != null;
    }
}
