using System.Reflection;
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

        private HitbloqPoolCellController? hitbloqPoolCell;
        private HitbloqPoolListEntry? hitbloqPoolListEntry;

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

        public void SetPool(HitbloqPoolListEntry hitbloqPoolListEntry)
        {
            this.hitbloqPoolListEntry = hitbloqPoolListEntry;
            if (hitbloqPoolCell != null)
            {
                hitbloqPoolCell.PopulateCell(hitbloqPoolListEntry);
            }
            NotifyPropertyChanged(nameof(Description));
            descriptionTextPage.ScrollTo(0, true);
        }

        #endregion

        #region Values

        [UIValue("description")]
        private string Description => $"Owners: {hitbloqPoolListEntry?.Author}\n\n" + (string.IsNullOrWhiteSpace(hitbloqPoolListEntry?.Description) ? "No Description available for this pool." : hitbloqPoolListEntry?.Description ?? "");

        #endregion
    }
}
