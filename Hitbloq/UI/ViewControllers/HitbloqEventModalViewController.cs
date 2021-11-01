using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Entries;
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
    internal class HitbloqEventModalViewController : INotifyPropertyChanged
    {
        private readonly EventSource eventSource;
        private readonly SpriteLoader spriteLoader;
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

        [UIParams]
        private readonly BSMLParserParams parserParams;

        public HitbloqEventModalViewController(EventSource eventSource, SpriteLoader spriteLoader, [InjectOptional] PlaylistManagerIHardlyKnowHer playlistManagerIHardlyKnowHer)
        {
            this.eventSource = eventSource;
            this.spriteLoader = spriteLoader;
            this.playlistManagerIHardlyKnowHer = playlistManagerIHardlyKnowHer;
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

            currentEvent = await eventSource.GetEventAsync();

            if (currentEvent != null && currentEvent.image != null && eventImage != null)
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
