using HMUI;
using BeatSaberMarkupLanguage;
using Hitbloq.Entries;
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
            
            if (!hitbloqRankedListViewController.isInViewControllerHierarchy)
            {
                SetLeftScreenViewController(hitbloqRankedListViewController, ViewController.AnimationType.In);
            }
            
            hitbloqPoolDetailViewController.SetPool(pool);
            hitbloqRankedListViewController.SetPool(pool.ID);
        }
        
        private void OnDetailDismissRequested()
        {
            if (hitbloqPoolDetailViewController.isInViewControllerHierarchy)
            {
                PopViewControllerFromNavigationController(hitbloqNavigationController, immediately: true);
            }
        }
        
        private void OnFlowDismissRequested()
        {
            parentFlowCoordinator.DismissFlowCoordinator(this, immediately: true);
        }

        protected override void BackButtonWasPressed(ViewController topViewController)
        {
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
