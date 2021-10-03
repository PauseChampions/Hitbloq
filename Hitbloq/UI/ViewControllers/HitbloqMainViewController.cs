using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System.Collections.Generic;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqMainView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqMainView.bsml")]
    internal class HitbloqMainViewController : BSMLAutomaticViewController
    {
        [UIValue("pools")]
        private List<object> pools = new List<object> { "None" };
    }
}
