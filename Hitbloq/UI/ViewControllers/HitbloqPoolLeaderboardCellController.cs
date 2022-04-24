using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using Hitbloq.Entries;
using Hitbloq.Other;
using Hitbloq.Utilities;
using HMUI;
using Tweening;
using UnityEngine;

namespace Hitbloq.UI.ViewControllers
{
    internal class HitbloqPoolLeaderboardCellController : TableCell, INotifyPropertyChanged
    {
        private HitbloqPoolLeaderboardEntry? poolLeaderboardEntry;
        
        private SpriteLoader spriteLoader = null!;
        private MaterialGrabber materialGrabber = null!;
        private TweeningManager uwuTweenyManager = null!;

        [UIComponent("profile-picture")]
        private readonly ImageView profilePicture = null!;

        [UIAction("#post-parse")]
        private void PostParse()
        {
            profilePicture.material = materialGrabber.NoGlowRoundEdge;
            
            fogBG = background.background.material;
            roundRectSmall = background.background.sprite;
            originalBackgroundColour = background.background.color;
        }

        public void SetRequiredUtils(SpriteLoader spriteLoader, MaterialGrabber materialGrabber, TweeningManager uwuTweenyManager)
        {
            this.spriteLoader = spriteLoader;
            this.materialGrabber = materialGrabber;
            this.uwuTweenyManager = uwuTweenyManager;
        }
        
        public HitbloqPoolLeaderboardCellController PopulateCell(HitbloqPoolLeaderboardEntry poolLeaderboardEntry)
        {
            if (poolLeaderboardEntry == this.poolLeaderboardEntry)
            {
                return this;
            }
            
            this.poolLeaderboardEntry = poolLeaderboardEntry;
            _ = FetchProfilePicture();
            _ = FetchBackground();

            NotifyPropertyChanged(nameof(Rank));
            NotifyPropertyChanged(nameof(Username));
            NotifyPropertyChanged(nameof(CR));
            NotifyPropertyChanged(nameof(RankChange));
            
            return this;
        }
        
        private async Task FetchProfilePicture()
        {
            var currentEntry = poolLeaderboardEntry;
            profilePicture.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;

            if (poolLeaderboardEntry is not {ProfilePictureURL:{}})
            {
                await spriteLoader.FetchSpriteFromResourcesAsync("Hitbloq.Images.Logo.png", sprite =>
                {
                    if (poolLeaderboardEntry == currentEntry)
                    {
                        profilePicture.sprite = sprite;
                    }
                });
                return;
            }
            
            await spriteLoader.DownloadSpriteAsync(poolLeaderboardEntry.ProfilePictureURL, sprite =>
            {
                if (poolLeaderboardEntry == currentEntry)
                {
                    profilePicture.sprite = sprite;
                }
            });
        }

        #region Background

        private Sprite? roundRectSmall;
        private Material? fogBG;
        
        private Color? originalBackgroundColour;
        private readonly Color customBackgroundColour = new(0.5f, 0.5f, 0.5f, 1f);
        
        [UIComponent("background")]
        private readonly Backgroundable background = null!;
        
        private async Task FetchBackground()
        {
            if (background.background is not ImageView bgImageView)
            {
                return;
            }
            
            var currentEntry = poolLeaderboardEntry;
            
            bgImageView.sprite = roundRectSmall;
            bgImageView.overrideSprite = roundRectSmall;
            bgImageView.color = originalBackgroundColour!.Value;
            bgImageView.material = fogBG;
            Accessors.GradientAccessor(ref bgImageView) = false;

            if (poolLeaderboardEntry is not {BannerImageURL:{}})
            {
                return;
            }
            
            await spriteLoader.DownloadSpriteAsync(poolLeaderboardEntry.BannerImageURL, sprite =>
            {
                if (poolLeaderboardEntry == currentEntry)
                {
                    bgImageView.sprite = sprite;
                    bgImageView.overrideSprite = sprite;
                    bgImageView.color = customBackgroundColour;
                    bgImageView.color0 = new Color(0.5f, 0.5f, 0.5f, 1f);
                    bgImageView.color1 = Color.white;
                    Accessors.GradientAccessor(ref bgImageView) = true;
                    bgImageView.material = materialGrabber.NoGlowRoundEdge;
                }
            });
        }

        #endregion

        [UIValue("rank")] 
        private string Rank => poolLeaderboardEntry?.Rank.ToString() ?? "";
        
        [UIValue("username")] 
        private string Username => poolLeaderboardEntry?.Username ?? "";
        
        [UIValue("cr")] 
        private string CR => poolLeaderboardEntry?.CR.ToString(CultureInfo.InvariantCulture) ?? "";
        
        [UIValue("rank-change")] 
        private string RankChange => poolLeaderboardEntry?.RankChange.ToString() ?? "";
        
        #region Property Changed
        
        public event PropertyChangedEventHandler? PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        
        #endregion
    }
}