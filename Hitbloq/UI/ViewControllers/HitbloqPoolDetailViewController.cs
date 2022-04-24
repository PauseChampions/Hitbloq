using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Other;
using Hitbloq.UI;
using HMUI;
using Tweening;
using UnityEngine;
using Zenject;

namespace MorePlaylists.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPoolDetailView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPoolDetailView.bsml")]
    internal class HitbloqPoolDetailViewController : BSMLAutomaticViewController
    {
        [Inject]
        private readonly SpriteLoader spriteLoader = null!;
        
        [Inject] 
        private readonly MaterialGrabber materialGrabber = null!;
        
        [Inject]
        private readonly TimeTweeningManager uwuTweenyManager = null!;
        
        [InjectOptional]
        private readonly PlaylistManagerIHardlyKnowHer? playlistManagerIHardlyKnowHer = null!;

        private HitbloqPoolCellController? hitbloqPoolCell;
        private HitbloqPoolListEntry? hitbloqPoolListEntry;
        private IBeatmapLevelPack? localPlaylist;
        private CancellationTokenSource? playlistSearchTokenSource;

        public event Action? FlowDismissRequested;

        [UIComponent("pool-cell")] 
        private readonly RectTransform? poolCellParentTransform = null!;
        
        [UIComponent("text-page")]
        private readonly TextPageScrollView descriptionTextPage = null!;

        #region Actions

        [UIAction("#post-parse")]
        private void PostParse()
        {
            rectTransform.anchorMax = new Vector2(0.5f, 1);
            hitbloqPoolCell = new GameObject("PoolCellDetail").AddComponent<HitbloqPoolCellController>();
            hitbloqPoolCell.transform.SetParent(poolCellParentTransform, false);
            hitbloqPoolCell.transform.SetSiblingIndex(0);
            hitbloqPoolCell.SetRequiredUtils(spriteLoader, materialGrabber, uwuTweenyManager);
            hitbloqPoolCell.interactable = false;
            BSMLParser.instance.Parse(Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqPoolCell.bsml"), hitbloqPoolCell.gameObject, hitbloqPoolCell);
        }

        [UIAction("download-click")]
        private void DownloadPressed() => _ = DownloadPoolAsync();

        private async Task DownloadPoolAsync()
        {
            var pool = hitbloqPoolListEntry;
            if (pool != null && playlistManagerIHardlyKnowHer != null)
            {
                pool.DownloadBlocked = true;
                NotifyPropertyChanged(nameof(DownloadInteractable));
                
                var localPlaylist = await playlistManagerIHardlyKnowHer.DownloadPlaylistFromPoolID(pool.ID);
                if (hitbloqPoolListEntry == pool)
                {
                    this.localPlaylist = localPlaylist;
                }
                
                pool.DownloadBlocked = false;
                NotifyPropertyChanged(nameof(DownloadInteractable));
                NotifyPropertyChanged(nameof(DownloadActive));
                NotifyPropertyChanged(nameof(GoToActive));
            }
        }
        
        [UIAction("go-to-playlist")]
        private void GoToPlaylist()
        {
            if (playlistManagerIHardlyKnowHer != null && localPlaylist != null)
            {
                FlowDismissRequested?.Invoke();
                playlistManagerIHardlyKnowHer.OpenPlaylist(localPlaylist);
            }
        }

        public void SetPool(HitbloqPoolListEntry hitbloqPoolListEntry)
        {
            this.hitbloqPoolListEntry = hitbloqPoolListEntry;
            localPlaylist = null;
            
            if (hitbloqPoolCell != null)
            {
                hitbloqPoolCell.PopulateCell(hitbloqPoolListEntry);
            }
            
            NotifyPropertyChanged(nameof(Description));
            NotifyPropertyChanged(nameof(DownloadInteractable));
            NotifyPropertyChanged(nameof(DownloadActive));
            NotifyPropertyChanged(nameof(GoToActive));
            
            descriptionTextPage.ScrollTo(0, true);
            
            playlistSearchTokenSource?.Cancel();
            playlistSearchTokenSource?.Dispose();
            playlistSearchTokenSource = new CancellationTokenSource();
            _ = FetchPlaylistAsync(hitbloqPoolListEntry.ID, playlistSearchTokenSource.Token);
        }

        private async Task FetchPlaylistAsync(string poolID, CancellationToken token)
        {
            if (playlistManagerIHardlyKnowHer == null)
            {
                return;
            }

            var localPlaylist = await playlistManagerIHardlyKnowHer.FindLocalPlaylistFromPoolID(poolID, token);

            if (localPlaylist != null)
            {
                this.localPlaylist = localPlaylist;
                NotifyPropertyChanged(nameof(DownloadActive));
                NotifyPropertyChanged(nameof(GoToActive));
            }
        }

        #endregion

        #region Values

        [UIValue("description")]
        private string Description => $"Owners: {hitbloqPoolListEntry?.Author}\n\n" + (string.IsNullOrWhiteSpace(hitbloqPoolListEntry?.Description) ? "No Description available for this pool." : hitbloqPoolListEntry?.Description ?? "");
        
        [UIValue("download-interactable")]
        public bool DownloadInteractable => hitbloqPoolListEntry is {DownloadBlocked: false};

        [UIValue("download-active")]
        public bool DownloadActive => playlistManagerIHardlyKnowHer is {CanOpenPlaylist: true} && hitbloqPoolListEntry != null && localPlaylist == null;

        [UIValue("go-to-active")]
        public bool GoToActive => playlistManagerIHardlyKnowHer != null && localPlaylist != null;

        #endregion
    }
}
