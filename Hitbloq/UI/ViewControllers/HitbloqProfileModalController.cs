using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Sources;
using HMUI;
using IPA.Utilities;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace Hitbloq.UI
{
    internal class HitbloqProfileModalController : IPoolUpdater, INotifyPropertyChanged
    {
        private readonly PoolInfoSource poolInfoSource;

        private HitbloqRankInfo _rankInfo;
        private HitbloqPoolInfo _poolInfo;

        private CancellationTokenSource poolInfoTokenSource;

        private bool parsed;

        [UIComponent("modal")]
        private ModalView modalView;

        [UIComponent("modal")]
        private readonly RectTransform modalTransform;

        private Vector3 modalPosition;

        [UIComponent("modal-profile-pic")]
        private ImageView modalProfilePic;

        [UIComponent("modal-info-vertical")]
        private Backgroundable modalInfoVertical;

        [UIParams]
        private readonly BSMLParserParams parserParams;

        public event PropertyChangedEventHandler PropertyChanged;

        private HitbloqRankInfo RankInfo
        {
            get => _rankInfo;
            set
            {
                _rankInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Username)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rank)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CR)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScoreCount)));
            }
        }

        private HitbloqPoolInfo PoolInfo
        {
            get => _poolInfo;
            set
            {
                _poolInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PoolName)));
            }
        }

        public HitbloqProfileModalController(PoolInfoSource poolInfoSource)
        {
            this.poolInfoSource = poolInfoSource;
        }

        public void Initialize()
        {
            parsed = false;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            parsed = true;

            Material noGlowRoundEdge = Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "UINoGlowRoundEdge");
            modalProfilePic.material = noGlowRoundEdge;

            ImageView verticalBackground = modalInfoVertical.background as ImageView;
            verticalBackground.color = new Color(0f, 0f, 0f, 0.75f);
        }

        private void Parse(Transform parentTransform)
        {
            if (!parsed)
            {
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqProfileModal.bsml"), parentTransform.gameObject, this);
                modalPosition = modalTransform.localPosition;
            }
            modalTransform.localPosition = modalPosition;
            modalView.SetField("_animateParentCanvas", true);
        }

        internal async void ShowModal(Transform parentTransform, HitbloqRankInfo rankInfo, string pool)
        {
            PoolUpdated(pool);
            RankInfo = rankInfo;
            Parse(parentTransform);
            parserParams.EmitEvent("close-modal");
            parserParams.EmitEvent("open-modal");
        }

        public async void PoolUpdated(string pool)
        {
            poolInfoTokenSource?.Cancel();
            poolInfoTokenSource = new CancellationTokenSource();
            PoolInfo = await poolInfoSource.GetPoolInfoAsync(pool, poolInfoTokenSource.Token);
        }

        [UIValue("username")]
        private string Username => RankInfo.username;

        [UIValue("pool-name")]
        private string PoolName => PoolInfo.shownName;

        [UIValue("rank")]
        private string Rank => $"{RankInfo.rank}";

        [UIValue("cr")]
        private string CR => $"{RankInfo.cr}";

        [UIValue("score-count")]
        private string ScoreCount => $"{RankInfo.scoreCount}";
    }
}
