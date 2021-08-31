using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using System.Collections.Generic;
using System.Linq;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPanel.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPanel.bsml")]
    internal class HitbloqPanelController : BSMLAutomaticViewController, IDifficultyBeatmapUpdater
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

        public void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo levelInfoEntry)
        {
            pools = new List<object>();
            if (levelInfoEntry != null)
            {
                foreach(var pool in levelInfoEntry.pools)
                {
                    pools.Add($"{pool.Key} - {pool.Value}⭐");
                }
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
