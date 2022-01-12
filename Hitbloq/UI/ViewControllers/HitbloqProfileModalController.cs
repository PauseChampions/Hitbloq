using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Other;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Hitbloq.UI
{
    internal class HitbloqProfileModalController : INotifyPropertyChanged
    {
        private readonly IPlatformUserModel platformUserModel;
        private readonly UserIDSource userIDSource;
        private readonly ProfileSource profileSource;
        private readonly FriendIDSource friendIDSource;
        private readonly FriendsLeaderboardSource friendsLeaderboardSource;
        private readonly RankInfoSource rankInfoSource;
        private readonly PoolInfoSource poolInfoSource;
        private readonly SpriteLoader spriteLoader;

        private bool _isFriend;
        private bool _isloading;
        private bool _addFriendInteractable;
        private string _addFriendHoverHint;
        private HitbloqRankInfo _rankInfo;
        private HitbloqPoolInfo _poolInfo;
        private HitbloqProfile _hitbloqProfile;

        private bool parsed;

        private Material fogBG;
        private Material noGlowRoundEdge;
        private Sprite roundRectSmall;

        private Color originalModalColour;
        private Color customModalColour;

        private int userID;
        private Sprite addFriend;
        private Sprite friendAdded;
        private const string ADD_FRIEND_PROMPT = "Add to friends list";
        private const string REMOVE_FRIEND_PROMPT = "Remove from friends list";
        private const string ALREADY_PROMPT = "You are already friends on ";

        private Vector3 modalPosition;
        private ImageView modalBackground;

        [UIComponent("modal")]
        private readonly ModalView modalView;

        [UIComponent("modal")]
        private readonly RectTransform modalTransform;

        [UIComponent("modal-profile-pic")]
        private readonly ImageView modalProfilePic;

        [UIComponent("modal-badge")]
        private readonly ImageView modalBadge;

        [UIComponent("add-friend")]
        private readonly ButtonIconImage addFriendButton;

        [UIComponent("modal-info-vertical")]
        private readonly Backgroundable modalInfoVertical;

        [UIParams]
        private readonly BSMLParserParams parserParams;

        public event PropertyChangedEventHandler PropertyChanged;
        
        private bool IsFriend
        {
            get => _isFriend;
            set
            {
                _isFriend = value;
                if (value)
                {
                    addFriendButton.image.sprite = friendAdded;
                    AddFriendHoverHint = REMOVE_FRIEND_PROMPT;
                }
                else
                {
                    addFriendButton.image.sprite = addFriend;
                    AddFriendHoverHint = ADD_FRIEND_PROMPT;
                }
            }
        }

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

                if (_hitbloqProfile != null)
                {
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
                }
            }
        }

        public HitbloqProfileModalController(IPlatformUserModel platformUserModel, UserIDSource userIDSource, ProfileSource profileSource, FriendIDSource friendIDSource,
            FriendsLeaderboardSource friendsLeaderboardSource, RankInfoSource rankInfoSource, PoolInfoSource poolInfoSource, SpriteLoader spriteLoader)
        {
            this.platformUserModel = platformUserModel;
            this.userIDSource = userIDSource;
            this.profileSource = profileSource;
            this.friendIDSource = friendIDSource;
            this.friendsLeaderboardSource = friendsLeaderboardSource;
            this.rankInfoSource = rankInfoSource;
            this.poolInfoSource = poolInfoSource;
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

            addFriendButton.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            addFriend = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.AddFriend.png");
            friendAdded = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.FriendAdded.png");

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

            modalProfilePic.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
            modalBadge.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;

            modalBackground.sprite = roundRectSmall;
            modalBackground.color = originalModalColour;
            modalBackground.material = fogBG;

            modalView.SetField("_animateParentCanvas", true);
        }

        internal async void ShowModalForSelf(Transform parentTransform, HitbloqRankInfo rankInfo, string pool)
        {
            Parse(parentTransform);
            HitbloqUserID userID = await userIDSource.GetUserIDAsync();

            if (userID.id == -1)
            {
                return;
            }

            parserParams.EmitEvent("close-modal");
            parserParams.EmitEvent("open-modal");

            IsLoading = true;

            RankInfo = rankInfo;
            PoolInfo = await poolInfoSource.GetPoolInfoAsync(pool);
            HitbloqProfile = await profileSource.GetProfileAsync(userID.id);
            addFriendButton.gameObject.SetActive(false);

            IsLoading = false;
        }

        internal async void ShowModalForUser(Transform parentTransform, int userID, string pool)
        {
            Parse(parentTransform);

            parserParams.EmitEvent("close-modal");
            parserParams.EmitEvent("open-modal");

            IsLoading = true;

            RankInfo = await rankInfoSource.GetRankInfoAsync(pool, userID);
            PoolInfo = await poolInfoSource.GetPoolInfoAsync(pool);
            HitbloqProfile = await profileSource.GetProfileAsync(userID);

            HitbloqUserID selfID = await userIDSource.GetUserIDAsync();
            addFriendButton.gameObject.SetActive(selfID.id != userID);
            HashSet<int> platformFriends = await friendIDSource.GetPlatformFriendIDsAsync();
            if (platformFriends.Contains(userID))
            {
                addFriendButton.image.sprite = friendAdded;
                AddFriendInteractable = false;
                AddFriendHoverHint = ALREADY_PROMPT + (await platformUserModel.GetUserInfo()).platform;
            }
            else
            {
                this.userID = userID;
                AddFriendInteractable = true;
                IsFriend = PluginConfig.Instance.Friends.Contains(userID);
            }

            IsLoading = false;
        }

        [UIAction("add-friend-click")]
        private void AddFriendClicked()
        {
            if (IsFriend)
            {
                PluginConfig.Instance.Friends.Remove(userID);
                PluginConfig.Instance.Changed();
            }
            else
            {
                PluginConfig.Instance.Friends.Add(userID);
                PluginConfig.Instance.Changed();
            }
            IsFriend = !IsFriend;
            friendsLeaderboardSource.ClearCache();
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

        [UIValue("add-friend-interactable")]
        private bool AddFriendInteractable
        {
            get => _addFriendInteractable;
            set
            {
                _addFriendInteractable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AddFriendInteractable)));
            }
        }

        [UIValue("add-friend-hover")]
        private string AddFriendHoverHint
        {
            get => _addFriendHoverHint;
            set
            {
                _addFriendHoverHint = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AddFriendHoverHint)));
            }
        }

        [UIValue("username")]
        private string Username
        {
            get
            {
                string userName = $"{RankInfo?.username}";
                if (userName.Length > 16)
                {
                    return $"{userName.Substring(0, 13)}...";
                }
                return userName;
            }
        }

        [UIValue("pool-name")]
        private string PoolName
        {
            get
            {
                string poolName = $"{PoolInfo?.shownName}";
                if (poolName.HasNonASCIIChars())
                {
                    poolName = $"{PoolInfo?.id}";
                }
                if (poolName.Length > 16)
                {
                    return $"{poolName.Substring(0, 13)}...";
                }
                return poolName;
            }
        }

        [UIValue("rank")]
        private string Rank => $"#{RankInfo?.rank}";

        [UIValue("cr")]
        private string CR => $"{RankInfo?.cr}";

        [UIValue("score-count")]
        private string ScoreCount => $"{RankInfo?.scoreCount}";
    }
}
