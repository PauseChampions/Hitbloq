using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System.Collections.Generic;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPanel.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPanel.bsml")]
    internal class HitbloqPanelController : BSMLAutomaticViewController
    {
        private HitbloqFlowCoordinator hitbloqFlowCoordinator;

        [UIValue("pools")]
        private List<object> pools = new List<object> { "Midspeed Acc" };

        [Inject]
        private void Inject(HitbloqFlowCoordinator hitbloqFlowCoordinator)
        {
            this.hitbloqFlowCoordinator = hitbloqFlowCoordinator;
        }

        [UIAction("clicked-logo")]
        private void LogoClicked()
        {
            hitbloqFlowCoordinator.Show();
        }
    }
}
