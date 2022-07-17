using HMUI;
using BeatSaberMarkupLanguage;
using Hitbloq.Entries;
using Hitbloq.UI.ViewControllers;
using MorePlaylists.UI;
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

        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            SetTitle("Hitbloq");
            showBackButton = true;
            SetViewControllersToNavigationController(hitbloqNavigationController, hitbloqPoolListViewController);
            ProvideInitialViewControllers(hitbloqNavigationController);
            hitbloqPoolListViewController.PoolSelectedEvent += OnPoolSelected;
            hitbloqPoolListViewController.DetailDismissRequested += OnDetailDismissRequested;
            hitbloqPoolDetailViewController.FlowDismissRequested += OnFlowDismissRequested;
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            hitbloqPoolListViewController.PoolSelectedEvent -= OnPoolSelected;
            hitbloqPoolListViewController.DetailDismissRequested -= OnDetailDismissRequested;
            hitbloqPoolDetailViewController.FlowDismissRequested -= OnFlowDismissRequested;
        }

        private void OnPoolSelected(HitbloqPoolListEntry pool)
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
        
        private void OnFlowDismissRequested()
        {
            parentFlowCoordinator.DismissFlowCoordinator(this, immediately: true);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
            hitbloqRankedListViewController.gameObject.SetActive(false);
            hitbloqPoolLeaderboardViewController.gameObject.SetActive(false);
            parentFlowCoordinator.DismissFlowCoordinator(this);
        }

        internal void Show()
        {
            parentFlowCoordinator = mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
            parentFlowCoordinator.PresentFlowCoordinator(this);
        }
    }
    
    internal class HitbloqNavigationController : NavigationController
    {
    }
}
