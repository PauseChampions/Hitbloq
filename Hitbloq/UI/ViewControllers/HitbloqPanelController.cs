using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Components.Settings;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using HMUI;
using IPA.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using UnityEngine;
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
        public event Action ClickedRankText;

        [UIComponent("container")]
        private readonly Backgroundable container;

        [UIComponent("hitbloq-logo")]
        private readonly ImageView logo;

        [UIComponent("separator")]
        private readonly ImageView separator;

        [UIComponent("dropdown-list")]
        private readonly DropDownListSetting dropDownListSetting;

        [Inject]
        private void Inject(HitbloqFlowCoordinator hitbloqFlowCoordinator, RankInfoSource rankInfoSource)
        {
            this.hitbloqFlowCoordinator = hitbloqFlowCoordinator;
            this.rankInfoSource = rankInfoSource;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            container.background.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            ImageView background = container.background as ImageView;
            background.color0 = Color.white;
            background.color1 = new Color(1f, 1f, 1f, 0f);
            background.color = Color.gray;
            background.SetField<ImageView, bool>("_gradient", true);
            background.SetField<ImageView, float>("_skew", 0.18f);

            logo.SetField<ImageView, float>("_skew", 0.18f);
            logo.SetVerticesDirty();

            separator.SetVerticesDirty();
            separator.SetField<ImageView, float>("_skew", 0.18f);
        }

        [UIAction("clicked-logo")]
        private void LogoClicked()
        {
            hitbloqFlowCoordinator.Show();
        }
        [UIAction("clicked-rank-text")]
        private void RankTextClicked()
        {
            ClickedRankText?.Invoke();
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
        private string PoolRankingText => $"<b>Pool Ranking:</b> #{rank} <size=75%>(<color=#aa6eff>{cr.ToString("F2")}cr</color>)";

        [UIValue("pools")]
        private List<object> pools = new List<object> { "None" };
    }
}
