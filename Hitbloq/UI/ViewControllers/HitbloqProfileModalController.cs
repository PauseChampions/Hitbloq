using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Entries;
using Hitbloq.Other;
using Hitbloq.Sources;
using HMUI;
using IPA.Utilities;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Hitbloq.UI
{
    internal class HitbloqProfileModalController : INotifyPropertyChanged
    {
        private readonly ProfileSource profileSource;
        private readonly PoolInfoSource poolInfoSource;
        private readonly SpriteLoader spriteLoader;

        private bool _isloading;
        private HitbloqRankInfo _rankInfo;
        private HitbloqPoolInfo _poolInfo;
        private HitbloqProfile _hitbloqProfile;

        private bool parsed;

        private Material fogBG;
        private Material noGlowRoundEdge;
        private Sprite roundRectSmall;

        private Color originalModalColour;
        private Color customModalColour;

        private Vector3 modalPosition;
        private ImageView modalBackground;

        [UIComponent("modal")]
        private ModalView modalView;

        [UIComponent("modal")]
        private readonly RectTransform modalTransform;

        [UIComponent("modal-profile-pic")]
        private ImageView modalProfilePic;

        [UIComponent("modal-badge")]
        private ImageView modalBadge;

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
                spriteLoader.DownloadSpriteAsync(_rankInfo.TierURL, (Sprite sprite) => modalBadge.sprite = sprite);
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

        private HitbloqProfile HitbloqProfile
        {
            get => _hitbloqProfile;
            set
            {
                _hitbloqProfile = value;
                spriteLoader.DownloadSpriteAsync(_hitbloqProfile.profilePictureURL, (Sprite sprite) => modalProfilePic.sprite = sprite);

                if (_hitbloqProfile.profileBackgroundURL != null)
                {
                    spriteLoader.DownloadSpriteAsync(_hitbloqProfile.profileBackgroundURL, (Sprite sprite) =>
                    {
                        modalBackground.sprite = sprite;
                        modalBackground.color = customModalColour;
                        modalBackground.material = noGlowRoundEdge;
                    });
                }
                else
                {
                    modalBackground.sprite = roundRectSmall;
                    modalBackground.color = originalModalColour;
                    modalBackground.material = fogBG;
                }
            }
        }

        public HitbloqProfileModalController(PoolInfoSource poolInfoSource, ProfileSource profileSource, SpriteLoader spriteLoader)
        {
            this.poolInfoSource = poolInfoSource;
            this.profileSource = profileSource;
            this.spriteLoader = spriteLoader;
        }

        public void Initialize()
        {
            parsed = false;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            parsed = true;
            modalView.gameObject.name = "HitbloqProfileModal";

            modalBackground = modalView.transform.Find("BG").GetComponent<ImageView>();
            fogBG = modalBackground.material;
            roundRectSmall = modalBackground.sprite;
            originalModalColour = modalBackground.color;
            customModalColour = new Color(0.5f, 0.5f, 0.5f, 1f);

            noGlowRoundEdge = Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "UINoGlowRoundEdge");
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

        internal async void ShowModalForSelf(Transform parentTransform, HitbloqRankInfo rankInfo, string pool)
        {
            Parse(parentTransform);

            parserParams.EmitEvent("close-modal");
            parserParams.EmitEvent("open-modal");

            IsLoading = true;

            RankInfo = rankInfo;
            PoolInfo = await poolInfoSource.GetPoolInfoAsync(pool);
            HitbloqProfile = await profileSource.GetProfileForSelfAsync();

            IsLoading = false;
        }

        [UIValue("is-loading")]
        private bool IsLoading
        {
            get => _isloading;
            set
            {
                _isloading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotLoading)));
            }
        }

        [UIValue("is-not-loading")]
        private bool IsNotLoading => !_isloading;

        [UIValue("username")]
        private string Username => $"{RankInfo?.username}";

        [UIValue("pool-name")]
        private string PoolName => $"{PoolInfo?.shownName}";

        [UIValue("rank")]
        private string Rank => $"{RankInfo?.rank}";

        [UIValue("cr")]
        private string CR => $"{RankInfo?.cr}";

        [UIValue("score-count")]
        private string ScoreCount => $"{RankInfo?.scoreCount}";
    }
}
