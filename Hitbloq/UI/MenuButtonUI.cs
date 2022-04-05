using System;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.MenuButtons;
using Zenject;

namespace Hitbloq.UI
{
    internal class MenuButtonUI : IInitializable, IDisposable
    {
        private readonly MenuButton menuButton;
        private readonly MainFlowCoordinator mainFlowCoordinator;
        private readonly HitbloqFlowCoordinator hitbloqFlowCoordinator;

        public MenuButtonUI(MainFlowCoordinator mainFlowCoordinator, HitbloqFlowCoordinator hitbloqFlowCoordinator)
        {
            menuButton = new MenuButton("Hitbloq", "Browse Pools & More!", MenuButtonClicked);
            this.mainFlowCoordinator = mainFlowCoordinator;
            this.hitbloqFlowCoordinator = hitbloqFlowCoordinator;
        }

        public void Initialize()
        {
            MenuButtons.instance.RegisterButton(menuButton);
        }

        public void Dispose()
        {
            if (MenuButtons.IsSingletonAvailable)
            {
                MenuButtons.instance.UnregisterButton(menuButton);
            }
        }

        private void MenuButtonClicked()
        {
            hitbloqFlowCoordinator.Show();
        }
    }
}