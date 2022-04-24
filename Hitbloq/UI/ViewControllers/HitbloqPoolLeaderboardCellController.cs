using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using Hitbloq.Entries;
using Hitbloq.Other;
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

            NotifyPropertyChanged(nameof(Rank));
            NotifyPropertyChanged(nameof(Username));
            NotifyPropertyChanged(nameof(CR));
            NotifyPropertyChanged(nameof(RankChange));
            
            return this;
        }
        
        private async Task FetchProfilePicture()
        {
            uwuTweenyManager.KillAllTweens(profilePicture);
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