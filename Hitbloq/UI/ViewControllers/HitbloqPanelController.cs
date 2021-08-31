using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Zenject;

namespace Hitbloq.UI
{
    [HotReload(RelativePathToLayout = @"..\Views\HitbloqPanel.bsml")]
    [ViewDefinition("Hitbloq.UI.Views.HitbloqPanel.bsml")]
    internal class HitbloqPanelController : BSMLAutomaticViewController, IDifficultyBeatmapUpdater
    {
        private HitbloqFlowCoordinator hitbloqFlowCoordinator;
        private RankInfoSource rankInfoSource;

        private int rank;
        private float cr;

        private CancellationTokenSource cancellationTokenSource;

        [UIComponent("dropdown-list")]
        private readonly DropDownListSetting dropDownListSetting;

        [Inject]
        private void Inject(HitbloqFlowCoordinator hitbloqFlowCoordinator, RankInfoSource rankInfoSource)
        {
            this.hitbloqFlowCoordinator = hitbloqFlowCoordinator;
            this.rankInfoSource = rankInfoSource;
        }

        [UIAction("clicked-logo")]
        private void LogoClicked()
        {
            hitbloqFlowCoordinator.Show();
        }

        public async void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo levelInfoEntry)
        {
            pools = new List<object>();
            rank = 0;
            cr = 0;

            if (levelInfoEntry != null)
            {
                foreach(var pool in levelInfoEntry.pools)
                {
                    pools.Add($"{pool.Key} - {pool.Value}⭐");
                }

                cancellationTokenSource?.Cancel();
                cancellationTokenSource = new CancellationTokenSource();
                HitbloqRankInfo rankInfo = await rankInfoSource.GetRankInfoAsync(levelInfoEntry.pools.Keys.First(), cancellationTokenSource.Token);
                rank = rankInfo.rank;
                cr = rankInfo.cr;
            }

            NotifyPropertyChanged(nameof(PoolRankingText));

            if (dropDownListSetting != null)
            {
                dropDownListSetting.values = pools.Count != 0 ? pools : new List<object> { "None" };
                dropDownListSetting.UpdateChoices();
                dropDownListSetting.dropdown.SelectCellWithIdx(0);
            }
        }

        [UIValue("pool-ranking-text")]
        private string PoolRankingText => $"Pool Ranking: #{rank} ({cr.ToString("F2")}cr)";

        [UIValue("pools")]
        private List<object> pools = new List<object> { "None" };
    }
}
