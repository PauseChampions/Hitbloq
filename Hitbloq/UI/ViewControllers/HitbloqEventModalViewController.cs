using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities.Async;
using UnityEngine;

namespace Hitbloq.UI
{
    internal class HitbloqEventModalViewController : NotifiableBase, INotifyViewActivated
    {
        private readonly IEventSource eventSource;
        private readonly SpriteLoader spriteLoader;
        private readonly HitbloqFlowCoordinator hitbloqFlowCoordinator;
        
        private HitbloqEvent? currentEvent;
        private bool parsed;
        
        [UIComponent("modal")]
        private ModalView? modalView;
        
        private Vector3? modalPosition;

        [UIComponent("modal")]
        private readonly RectTransform? modalTransform = null!;

        [UIComponent("event-image")]
        private readonly ImageView? eventImage = null!;

        [UIComponent("text-page")]
        private readonly TextPageScrollView? descriptionTextPage = null!;

        [UIParams]
        private readonly BSMLParserParams? parserParams = null!;

        public HitbloqEventModalViewController(IEventSource eventSource, SpriteLoader spriteLoader, HitbloqFlowCoordinator hitbloqFlowCoordinator)
        {
            this.eventSource = eventSource;
            this.spriteLoader = spriteLoader;
            this.hitbloqFlowCoordinator = hitbloqFlowCoordinator;
        }

        public void ViewActivated(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling) =>
            _ = ViewActivatedAsync(hitbloqLeaderboardViewController, firstActivation);

        private async Task ViewActivatedAsync(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, bool firstActivation)
        {
            if (firstActivation)
            {
                var hitbloqEvent = await eventSource.GetAsync();
                if (hitbloqEvent != null && hitbloqEvent.ID != -1)
                {
                    if (!PluginConfig.Instance.ViewedEvents.Contains(hitbloqEvent.ID))
                    {
                        await UnityMainThreadTaskScheduler.Factory.StartNew(() => ShowModal(hitbloqLeaderboardViewController.transform));
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
        private void PostParse() => _ = PostParseAsync();

        private async Task PostParseAsync()
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
                hitbloqFlowCoordinator.ShowAndOpenPoolWithID(currentEvent!.Pool);
            }
        }

        [UIValue("event-title")]
        private string EventTitle => $"{currentEvent?.Title}";

        [UIValue("event-description")]
        private string EventDescription => $"{currentEvent?.Description}";
        
        [UIValue("pool-exists")]
        private bool PoolExists => currentEvent is {Pool: { }};
    }
}
