using System;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace Hitbloq.UI
{
	internal class MenuButtonUI : IInitializable, IDisposable
	{
		private readonly HitbloqFlowCoordinator _hitbloqFlowCoordinator;
		private readonly MainFlowCoordinator _mainFlowCoordinator;
		private readonly MenuButton _menuButton;

		public MenuButtonUI(MainFlowCoordinator mainFlowCoordinator, HitbloqFlowCoordinator hitbloqFlowCoordinator)
		{
			_menuButton = new MenuButton("Hitbloq", "Browse Pools & More!", MenuButtonClicked);
			_mainFlowCoordinator = mainFlowCoordinator;
			_hitbloqFlowCoordinator = hitbloqFlowCoordinator;
		}

		public void Dispose()
		{
#if HITBLOQ_BS_1_29_1
			if (MenuButtons.IsSingletonAvailable)
			{
				MenuButtons.instance.UnregisterButton(_menuButton);
			}
#else
			MenuButtons.Instance.UnregisterButton(_menuButton);
#endif
		}

		public void Initialize()
		{
#if HITBLOQ_BS_1_29_1
			MenuButtons.instance.RegisterButton(_menuButton);
#else
			MenuButtons.Instance.RegisterButton(_menuButton);
#endif
		}

		private void MenuButtonClicked()
		{
			_hitbloqFlowCoordinator.Show();
		}
	}
}
