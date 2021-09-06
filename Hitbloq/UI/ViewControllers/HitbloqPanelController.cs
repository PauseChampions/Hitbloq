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
    internal class HitbloqPanelController : BSMLAutomaticViewController, IDifficultyBeatmapUpdater, IPoolUpdater
    {
        private HitbloqFlowCoordinator hitbloqFlowCoordinator;
        private RankInfoSource rankInfoSource;
        private PoolInfoSource poolInfoSource;

        private int rank;
        private float cr;
        private List<string> poolNames;

        private string _promptText;
        private bool _loadingActive;

        private CancellationTokenSource poolInfoTokenSource;
        private CancellationTokenSource rankInfoTokenSource;
        public event Action<string> PoolChangedEvent;

        [UIComponent("container")]
        private readonly Backgroundable container;

        [UIComponent("hitbloq-logo")]
        private readonly ImageView logo;

        [UIComponent("separator")]
        private readonly ImageView separator;

        [UIComponent("dropdown-list")]
        private readonly DropDownListSetting dropDownListSetting;

        [UIComponent("dropdown-list")]
        private readonly RectTransform dropDownListTransform;

        [Inject]
        private void Inject(HitbloqFlowCoordinator hitbloqFlowCoordinator, RankInfoSource rankInfoSource, PoolInfoSource poolInfoSource)
        {
            this.hitbloqFlowCoordinator = hitbloqFlowCoordinator;
            this.rankInfoSource = rankInfoSource;
            this.poolInfoSource = poolInfoSource;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            container.background.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
            ImageView background = container.background as ImageView;
            background.color0 = Color.white;
            background.color1 = new Color(1f, 1f, 1f, 0f);
            background.color = Color.gray;
            background.SetField("_gradient", true);
            background.SetField("_skew", 0.18f);

            logo.SetField("_skew", 0.18f);
            logo.SetVerticesDirty();

            separator.SetVerticesDirty();
            separator.SetField("_skew", 0.18f);

            CurvedTextMeshPro dropdownText = dropDownListTransform.GetComponentInChildren<CurvedTextMeshPro>();
            dropdownText.fontSize = 3.5f;
            dropdownText.transform.localPosition = new Vector3(-1.5f, 0, 0);
        }

        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            dropDownListSetting.dropdown.Hide(false);
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
        }

        [UIAction("pool-changed")]
        private void PoolChanged(string formattedPool)
        {
            PoolChangedEvent?.Invoke(poolNames[dropDownListSetting.dropdown.selectedIndex]);
        }

        public async void DifficultyBeatmapUpdated(IDifficultyBeatmap difficultyBeatmap, HitbloqLevelInfo levelInfoEntry)
        {
            poolInfoTokenSource?.Cancel();
            poolInfoTokenSource = new CancellationTokenSource();

            pools = new List<object>();
            rank = 0;
            cr = 0;

            if (levelInfoEntry != null)
            {
                foreach(var pool in levelInfoEntry.pools)
                {
                    HitbloqPoolInfo poolInfo = await poolInfoSource.GetPoolInfoAsync(pool.Key, poolInfoTokenSource.Token);
                    pools.Add($"{poolInfo.shownName} - {pool.Value}⭐");
                }
                poolNames = levelInfoEntry.pools.Keys.ToList();
                PoolUpdated(poolNames.First());
            }
            else
            {
                poolNames = new List<string> { "None" };
            }

            if (dropDownListSetting != null)
            {
                dropDownListSetting.values = pools.Count != 0 ? pools : new List<object> { "None" };
                dropDownListSetting.UpdateChoices();
                dropDownListSetting.dropdown.SelectCellWithIdx(0);

                PromptText = "";
                LoadingActive = false;
            }
        }

        public async void PoolUpdated(string pool)
        {
            rankInfoTokenSource?.Cancel();
            rankInfoTokenSource = new CancellationTokenSource();
            HitbloqRankInfo rankInfo = await rankInfoSource.GetRankInfoAsync(pool, rankInfoTokenSource.Token);
            rank = rankInfo.rank;
            cr = rankInfo.cr;
            NotifyPropertyChanged(nameof(PoolRankingText));
        }

        [UIValue("prompt-text")]
        public string PromptText
        {
            get => _promptText;
            set
            {
                _promptText = value;
                NotifyPropertyChanged(nameof(PromptText));
            }
        }

        [UIValue("loading-active")]
        public bool LoadingActive
        {
            get => _loadingActive;
            set
            {
                _loadingActive = value;
                NotifyPropertyChanged(nameof(LoadingActive));
            }
        }

        [UIValue("pool-ranking-text")]
        private string PoolRankingText => $"<b>Pool Ranking:</b> #{rank} <size=75%>(<color=#6772E5>{cr.ToString("F2")}cr</color>)";

        [UIValue("pools")]
        private List<object> pools = new List<object> { "None" };
    }
}
