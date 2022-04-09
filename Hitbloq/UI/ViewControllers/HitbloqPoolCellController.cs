using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Animations;
using BeatSaberMarkupLanguage.Attributes;
using Hitbloq.Entries;
using Hitbloq.Other;
using HMUI;
using TMPro;
using Tweening;
using UnityEngine;

namespace Hitbloq.UI
{
    internal class HitbloqPoolCellController : TableCell, INotifyPropertyChanged
    {
        private HitbloqPoolListEntry? poolListEntry;

        private SpriteLoader spriteLoader = null!;
        private MaterialGrabber materialGrabber = null!;
        private TweeningManager uwuTweenyManager = null!;
        
        [UIComponent("banner-image")]
        private readonly ImageView bannerImage = null!;
        
        [UIComponent("pool-name-text")]
        private readonly TextMeshProUGUI poolNameText = null!;
        
        [UIAction("#post-parse")]
        private void PostParse()
        {
            bannerImage.material = materialGrabber.NoGlowRoundEdge;
        }

        public void SetRequiredUtils(SpriteLoader spriteLoader, MaterialGrabber materialGrabber, TweeningManager uwuTweenyManager)
        {
            this.spriteLoader = spriteLoader;
            this.materialGrabber = materialGrabber;
            this.uwuTweenyManager = uwuTweenyManager;
        }

        public HitbloqPoolCellController PopulateCell(HitbloqPoolListEntry poolListEntry)
        {
            this.poolListEntry = poolListEntry;
            _ = FetchBanner();
            
            NotifyPropertyChanged(nameof(PoolName));
            NotifyPropertyChanged(nameof(ShowBannerTitle));
            NotifyPropertyChanged(nameof(Popularity));
            NotifyPropertyChanged(nameof(PlayerCount));
            
            poolNameText.alpha = 1;
            return this;
        }

        private async Task FetchBanner()
        {
            if (poolListEntry is not {BannerImageURL:{}})
            {
                return;
            }

            AnimationStateUpdater? stateUpdater = null;
            
            await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
            {
                stateUpdater = bannerImage.GetComponent<AnimationStateUpdater>() ?? bannerImage.gameObject.AddComponent<AnimationStateUpdater>();
                stateUpdater.image = bannerImage;
                stateUpdater.controllerData = AnimationController.instance.loadingAnimation;
            });

            var currentEntry = poolListEntry;
            
            await spriteLoader.DownloadSpriteAsync(poolListEntry.BannerImageURL, sprite =>
            {
                if (poolListEntry == currentEntry)
                {
                    if (stateUpdater != null)
                    {
                        DestroyImmediate(stateUpdater);
                    }
                    bannerImage.sprite = sprite;   
                }
            });
        }

        [UIValue("pool-name")] 
        private string PoolName => $"{poolListEntry?.Title}";

        [UIValue("show-banner-title")] 
        private bool ShowBannerTitle => (!poolListEntry?.BannerTitleHide ?? true) || highlighted || selected;
        
        [UIValue("popularity")] 
        private string Popularity => $"📈 {poolListEntry?.Popularity}";
        
        [UIValue("player-count")] 
        private string PlayerCount => $"👥 {poolListEntry?.PlayerCount}";

        #region Highlight and Selection

        private readonly Color highlightedColor = new(0.25f, 0.25f, 0.25f, 1);
        
        protected override void SelectionDidChange(TransitionType transitionType) => RefreshBackground();

        protected override void HighlightDidChange(TransitionType transitionType) => RefreshBackground();

        private void RefreshBackground()
        {
            uwuTweenyManager.KillAllTweens(this);
            if (selected)
            {
                poolNameText.alpha = 1;
                bannerImage.color = Color.gray;
            }
            else if (highlighted)
            {
                var currentColor = bannerImage.color;
                
                if (poolListEntry?.BannerTitleHide ?? false)
                {
                    poolNameText.alpha = 0;
                }
                
                var tween = new FloatTween(0, 1, val =>
                {
                    bannerImage.color = Color.Lerp(currentColor, highlightedColor, val);
                    if (poolListEntry?.BannerTitleHide ?? false)
                    {
                        poolNameText.alpha = val;
                    }
                }, 0.25f, EaseType.Linear);
                
                uwuTweenyManager.AddTween(tween, this);
            }
            else
            {
                bannerImage.color = Color.white;
            }
            NotifyPropertyChanged(nameof(ShowBannerTitle));
        }

        #endregion
        
        #region Property Changed
        
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}