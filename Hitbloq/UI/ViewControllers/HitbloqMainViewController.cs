using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqMainView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqMainView.bsml")]
    internal class HitbloqMainViewController : BSMLAutomaticViewController
    {
    }
}
