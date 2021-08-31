using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPanel.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPanel.bsml")]
    internal class HitbloqPanelController : BSMLAutomaticViewController, ILeaderboardEntriesUpdater
    {
        private HitbloqFlowCoordinator hitbloqFlowCoordinator;

        [UIComponent("dropdown-list")]
        private readonly DropDownListSetting dropDownListSetting;

        [UIValue("pools")]
        private List<object> pools = new List<object> { "None" };

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

        public void LeaderboardEntriesUpdated(List<Entries.LeaderboardEntry> leaderboardEntries)
        {
            if (leaderboardEntries != null && leaderboardEntries.Count != 0)
            {
                pools = leaderboardEntries[0].cr.Keys.Cast<object>().ToList();
            }
            else
            {
                pools = new List<object>();
            }

            if (dropDownListSetting != null)
            {
                dropDownListSetting.values = pools.Count != 0 ? pools : new List<object> { "None" };
                dropDownListSetting.UpdateChoices();
                dropDownListSetting.dropdown.SelectCellWithIdx(0);
            }
        }
    }
}
