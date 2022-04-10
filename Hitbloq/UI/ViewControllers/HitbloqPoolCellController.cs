using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
        private bool spriteDownloaded;

        private SpriteLoader spriteLoader = null!;
        private MaterialGrabber materialGrabber = null!;
        private TweeningManager uwuTweenyManager = null!;
        
        [UIComponent("banner-image")]
        private readonly ImageView bannerImage = null!;

        [UIComponent("pool-name-text")]
        private readonly TextMeshProUGUI poolNameText = null!;
        
        [UIComponent("description-text")]
        private readonly TextMeshProUGUI descriptionText = null!;
        
        [UIComponent("popularity-text")]
        private readonly TextMeshProUGUI popularityText = null!;
                
        [UIComponent("player-count-text")]
        private readonly TextMeshProUGUI playerCountText = null!;
        
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
            NotifyPropertyChanged(nameof(Description));
            NotifyPropertyChanged(nameof(Popularity));
            NotifyPropertyChanged(nameof(PlayerCount));
            
            poolNameText.alpha = 1;
            return this;
        }

        private async Task FetchBanner()
        {
            uwuTweenyManager.KillAllTweens(this);
            spriteDownloaded = false;
            NotifyPropertyChanged(nameof(ShowBannerTitle));
            var currentEntry = poolListEntry;
            
            await spriteLoader.FetchSpriteFromResourcesAsync("Hitbloq.Images.DefaultPoolBanner.png", sprite =>
            {
                if (poolListEntry == currentEntry)
                {
                    bannerImage.sprite = sprite;
                }
            });
            
            if (poolListEntry is not {BannerImageURL:{}})
            {
                return;
            }
            
            await spriteLoader.DownloadSpriteAsync(poolListEntry.BannerImageURL, sprite =>
            {
                if (poolListEntry == currentEntry)
                {
                    bannerImage.color = new Color(1, 1, 1, 0);

                    bannerImage.sprite = sprite;   
                    spriteDownloaded = true;
                    NotifyPropertyChanged(nameof(ShowBannerTitle));

                    var tween = new FloatTween(0, 1, val =>
                    {
                        bannerImage.color = new Color(1, 1, 1, val);
                    }, 0.5f, EaseType.Linear);
                
                    uwuTweenyManager.AddTween(tween, this);
                }
            });
        }

        [UIValue("pool-name")] 
        private string PoolName => $"{poolListEntry?.Title}";

        [UIValue("show-banner-title")] 
        private bool ShowBannerTitle => (!poolListEntry?.BannerTitleHide ?? true) || !spriteDownloaded || highlighted || selected;
        
        [UIValue("description")] 
        private string Description => $"{poolListEntry?.ShortDescription}";
        
        [UIValue("popularity")] 
        private string Popularity => $"📈 {poolListEntry?.Popularity}";
        
        [UIValue("player-count")] 
        private string PlayerCount => $"👥 {poolListEntry?.PlayerCount}";

        #region Highlight and Selection

        private readonly Color selectedColor = new(0.25f, 0.25f, 0.25f, 1);
        private readonly Color textSelectedColor = new(0, 0.7529412f, 1, 1);
        private readonly Color textColor = new Color(1, 1, 1, 0.7490196f);
        
        protected override void SelectionDidChange(TransitionType transitionType) => RefreshBackground();

        protected override void HighlightDidChange(TransitionType transitionType) => RefreshBackground();

        private void RefreshBackground()
        {
            uwuTweenyManager.KillAllTweens(this);
            if (selected)
            {
                poolNameText.alpha = 1;
                bannerImage.color = selectedColor;
                descriptionText.color = textSelectedColor;
                popularityText.color = textSelectedColor;
                playerCountText.color = textSelectedColor;
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
                    bannerImage.color = Color.Lerp(currentColor, Color.gray, val);
                    if (poolListEntry?.BannerTitleHide ?? false)
                    {
                        poolNameText.alpha = val;
                    }
                }, 0.25f, EaseType.Linear);
                
                uwuTweenyManager.AddTween(tween, this);
                descriptionText.color = Color.white;
                popularityText.color = Color.white;
                playerCountText.color = Color.white;
            }
            else
            {
                bannerImage.color = Color.white;     
                descriptionText.color = textColor;
                popularityText.color = textColor;
                playerCountText.color = textColor;
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