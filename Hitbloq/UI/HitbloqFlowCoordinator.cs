using HMUI;
using BeatSaberMarkupLanguage;
using Hitbloq.Entries;
using MorePlaylists.UI;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI
{
    internal class HitbloqFlowCoordinator : FlowCoordinator
    {
        [Inject]
        private readonly MainFlowCoordinator mainFlowCoordinator = null!;
        
        private FlowCoordinator? parentFlowCoordinator;
        
        [Inject]
        private readonly HitbloqNavigationController hitbloqNavigationController = null!;
        
        [Inject]
        private readonly HitbloqPoolListViewController hitbloqPoolListViewController = null!;
        
        [Inject]
        private readonly HitbloqPoolDetailViewController hitbloqPoolDetailViewController = null!;
        
        [Inject]
        private readonly HitbloqRankedListViewController hitbloqRankedListViewController = null!;
        
        [Inject]
        private readonly HitbloqPoolLeaderboardViewController hitbloqPoolLeaderboardViewController = null!;
        
        [Inject]
        private readonly HitbloqInfoViewController hitbloqInfoViewController = null!;
        
        [Inject]
        private readonly PopupModalsController popupModalsController = null!;

        private bool flowAnimationComplete;
        private HitbloqPoolListEntry? currentPool;

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            SetTitle("Hitbloq");
            showBackButton = true;
            flowAnimationComplete = false;
            currentPool = null;

            SetViewControllersToNavigationController(hitbloqNavigationController, hitbloqPoolListViewController);
            ProvideInitialViewControllers(hitbloqNavigationController, bottomScreenViewController: hitbloqInfoViewController);
            
            hitbloqPoolListViewController.PoolSelectedEvent += OnPoolSelected;
            hitbloqPoolListViewController.DetailDismissRequested += OnDetailDismissRequested;
            hitbloqPoolDetailViewController.FlowDismissRequested += OnFlowDismissRequested;
            hitbloqInfoViewController.URLOpenRequested += OnURLOpenRequested;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            hitbloqPoolListViewController.PoolSelectedEvent -= OnPoolSelected;
            hitbloqPoolListViewController.DetailDismissRequested -= OnDetailDismissRequested;
            hitbloqPoolDetailViewController.FlowDismissRequested -= OnFlowDismissRequested;
            hitbloqInfoViewController.URLOpenRequested -= OnURLOpenRequested;
        }

        private void OnPoolSelected(HitbloqPoolListEntry pool)
        {
            if (flowAnimationComplete)
            {
                if (!hitbloqPoolDetailViewController.isInViewControllerHierarchy)
                {
                    PushViewControllerToNavigationController(hitbloqNavigationController, hitbloqPoolDetailViewController);
                }
            
                SetLeftScreenViewController(hitbloqRankedListViewController, ViewController.AnimationType.In);
                SetRightScreenViewController(hitbloqPoolLeaderboardViewController, ViewController.AnimationType.In);   
                
                hitbloqPoolDetailViewController.SetPool(pool);
                hitbloqRankedListViewController.SetPool(pool.ID);
                hitbloqPoolLeaderboardViewController.SetPool(pool.ID);
            }
            
            currentPool = pool;
        }
        
        private void OnDetailDismissRequested()
        {
            if (hitbloqPoolDetailViewController.isInViewControllerHierarchy)
            {
                PopViewControllerFromNavigationController(hitbloqNavigationController, immediately: true);
            }
            
            if (hitbloqRankedListViewController.isInViewControllerHierarchy)
            {
                SetLeftScreenViewController(null, ViewController.AnimationType.Out);
            }

            if (hitbloqPoolLeaderboardViewController.isInViewControllerHierarchy)
            {
                SetRightScreenViewController(null, ViewController.AnimationType.Out);
            }
        }
        
        private void OnFlowDismissRequested() => parentFlowCoordinator.DismissFlowCoordinator(this, immediately: true);

        private void OnURLOpenRequested(string url) => popupModalsController.ShowYesNoModal
        (hitbloqPoolListViewController.rectTransform, $"Would you like to open\n{url}", () =>
        {
            Application.OpenURL(url);
        });

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            hitbloqRankedListViewController.gameObject.SetActive(false);
            hitbloqPoolLeaderboardViewController.gameObject.SetActive(false);
            hitbloqInfoViewController.gameObject.SetActive(false);
            parentFlowCoordinator.DismissFlowCoordinator(this);
        }

        internal void Show()
        {
            parentFlowCoordinator = mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
            parentFlowCoordinator.PresentFlowCoordinator(this, () =>
            {
                flowAnimationComplete = true;
                if (currentPool != null)
                {
                    OnPoolSelected(currentPool);
                }
            });
        }
        
        internal void ShowAndOpenPoolWithID(string? poolID)
        {
            hitbloqPoolListViewController.poolToOpen = poolID;
            Show();
        }
    }
    
    internal class HitbloqNavigationController : NavigationController
    {
    }
}
