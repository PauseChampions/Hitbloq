using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqInfoView.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqInfoView.bsml")]
    internal class HitbloqInfoViewController : BSMLAutomaticViewController
    {
    }
}
