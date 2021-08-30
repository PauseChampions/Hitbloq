using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\MainLeaderboardView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.MainLeaderboardView.bsml")]
    internal class HitbloqLeaderboardViewController : BSMLAutomaticViewController
    {
        
    }
}
