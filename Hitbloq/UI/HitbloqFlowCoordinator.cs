using BeatSaberMarkupLanguage;
using Hitbloq.Entries;
using Hitbloq.UI.ViewControllers;
using HMUI;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI
{
	internal class HitbloqFlowCoordinator : FlowCoordinator
	{
		[Inject]
		private readonly HitbloqInfoViewController _hitbloqInfoViewController = null!;

		[Inject]
		private readonly HitbloqNavigationController _hitbloqNavigationController = null!;

		[Inject]
		private readonly HitbloqPoolDetailViewController _hitbloqPoolDetailViewController = null!;

		[Inject]
		private readonly HitbloqPoolLeaderboardViewController _hitbloqPoolLeaderboardViewController = null!;

		[Inject]
		private readonly HitbloqPoolListViewController _hitbloqPoolListViewController = null!;

		[Inject]
		private readonly HitbloqRankedListViewController _hitbloqRankedListViewController = null!;

		[Inject]
		private readonly MainFlowCoordinator _mainFlowCoordinator = null!;

		[Inject]
		private readonly PopupModalsController _popupModalsController = null!;

		private HitbloqPoolListEntry? _currentPool;

		private bool _flowAnimationComplete;

		private FlowCoordinator? _parentFlowCoordinator;

		protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			SetTitle("Hitbloq");
			showBackButton = true;
			_flowAnimationComplete = false;
			_currentPool = null;

			SetViewControllersToNavigationController(_hitbloqNavigationController, _hitbloqPoolListViewController);
			ProvideInitialViewControllers(_hitbloqNavigationController, bottomScreenViewController: _hitbloqInfoViewController);

			_hitbloqPoolListViewController.PoolSelectedEvent += OnPoolSelected;
			_hitbloqPoolListViewController.DetailDismissRequested += OnDetailDismissRequested;
			_hitbloqPoolDetailViewController.FlowDismissRequested += OnFlowDismissRequested;
			_hitbloqInfoViewController.URLOpenRequested += OnURLOpenRequested;
		}

		protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
		{
			base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
			_hitbloqPoolListViewController.PoolSelectedEvent -= OnPoolSelected;
			_hitbloqPoolListViewController.DetailDismissRequested -= OnDetailDismissRequested;
			_hitbloqPoolDetailViewController.FlowDismissRequested -= OnFlowDismissRequested;
			_hitbloqInfoViewController.URLOpenRequested -= OnURLOpenRequested;
		}

		private void OnPoolSelected(HitbloqPoolListEntry pool)
		{
			if (_flowAnimationComplete)
			{
				if (!_hitbloqPoolDetailViewController.isInViewControllerHierarchy)
				{
					PushViewControllerToNavigationController(_hitbloqNavigationController, _hitbloqPoolDetailViewController);
				}

				SetLeftScreenViewController(_hitbloqRankedListViewController, ViewController.AnimationType.In);
				SetRightScreenViewController(_hitbloqPoolLeaderboardViewController, ViewController.AnimationType.In);

				_hitbloqPoolDetailViewController.SetPool(pool);
				_hitbloqRankedListViewController.SetPool(pool.ID);
				_hitbloqPoolLeaderboardViewController.SetPool(pool.ID);
			}

			_currentPool = pool;
		}

		private void OnDetailDismissRequested()
		{
			if (_hitbloqPoolDetailViewController.isInViewControllerHierarchy)
			{
				PopViewControllerFromNavigationController(_hitbloqNavigationController, immediately: true);
			}

			if (_hitbloqRankedListViewController.isInViewControllerHierarchy)
			{
				SetLeftScreenViewController(null, ViewController.AnimationType.Out);
			}

			if (_hitbloqPoolLeaderboardViewController.isInViewControllerHierarchy)
			{
				SetRightScreenViewController(null, ViewController.AnimationType.Out);
			}
		}

		private void OnFlowDismissRequested()
		{
			_parentFlowCoordinator.DismissFlowCoordinator(this, immediately: true);
		}

		private void OnURLOpenRequested(string url)
		{
			_popupModalsController.ShowYesNoModal(_hitbloqPoolListViewController.rectTransform, $"Would you like to open\n{url}", () => { Application.OpenURL(url); });
		}

		protected override void BackButtonWasPressed(ViewController topViewController)
		{
			_hitbloqRankedListViewController.gameObject.SetActive(false);
			_hitbloqPoolLeaderboardViewController.gameObject.SetActive(false);
			_hitbloqInfoViewController.gameObject.SetActive(false);
			_parentFlowCoordinator.DismissFlowCoordinator(this);
		}

		internal void Show()
		{
			_parentFlowCoordinator = _mainFlowCoordinator.YoungestChildFlowCoordinatorOrSelf();
			_parentFlowCoordinator.PresentFlowCoordinator(this, () =>
			{
				_flowAnimationComplete = true;
				if (_currentPool != null)
				{
					OnPoolSelected(_currentPool);
				}
			});
		}

		internal void ShowAndOpenPoolWithID(string? poolID)
		{
			_hitbloqPoolListViewController.poolToOpen = poolID;
			Show();
		}
	}

	internal class HitbloqNavigationController : NavigationController
	{
	}
}