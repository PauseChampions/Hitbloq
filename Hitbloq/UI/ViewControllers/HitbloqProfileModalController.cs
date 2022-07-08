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
using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
        private readonly MaterialGrabber materialGrabber;

        private readonly SemaphoreSlim modalSemaphore = new SemaphoreSlim(1, 1);
        private CancellationTokenSource? modalTokenSource;

        private bool isFriend;
        private bool isLoading;
        private bool addFriendInteractable;
        private string addFriendHoverHint = "";
        private HitbloqRankInfo? rankInfo;
        private HitbloqPoolInfo? poolInfo;
        private HitbloqProfile? hitbloqProfile;

        private bool parsed;

        private Material? fogBG;
        private Sprite? roundRectSmall;

        private Color? originalModalColour;
        private readonly Color customModalColour = new(0.5f, 0.5f, 0.5f, 1f);

        private int userID;
        private Sprite? addFriend;
        private Sprite? friendAdded;
        private const string kAddFriendPrompt = "Add to friends list";
        private const string kRemoveFriendPrompt = "Remove from friends list";
        private const string kAlreadyFriendPrompt = "You are already friends on ";

        private Vector3? modalPosition;
        private ImageView? modalBackground;

        [UIComponent("modal")]
        private ModalView? modalView;

        [UIComponent("modal")]
        private readonly RectTransform? modalTransform = null!;

        [UIComponent("modal-profile-pic")]
        private readonly ImageView? modalProfilePic = null!;

        [UIComponent("modal-badge")]
        private readonly ImageView? modalBadge = null!;

        [UIComponent("add-friend")]
        private readonly ButtonIconImage? addFriendButton = null!;

        [UIComponent("modal-info-vertical")]
        private readonly Backgroundable? modalInfoVertical = null!;

        [UIParams]
        private readonly BSMLParserParams? parserParams = null!;

        public event PropertyChangedEventHandler? PropertyChanged;
        
        private bool IsFriend
        {
            get => isFriend;
            set
            {
                isFriend = value;

                if (addFriendButton != null)
                {
                    if (value)
                    {
                        addFriendButton.image.sprite = friendAdded;
                        AddFriendHoverHint = kRemoveFriendPrompt;
                    }
                    else
                    {
                        addFriendButton.image.sprite = addFriend;
                        AddFriendHoverHint = kAddFriendPrompt;
                    }   
                }
            }
        }

        private HitbloqRankInfo? RankInfo
        {
            get => rankInfo;
            set
            {
                rankInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Username)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rank)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CR)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScoreCount)));

                if (modalBadge != null && rankInfo != null && modalTokenSource != null)
                {
                    _ = spriteLoader.DownloadSpriteAsync(rankInfo.TierURL, sprite => modalBadge.sprite = sprite, modalTokenSource.Token);
                }
            }
        }

        private HitbloqPoolInfo? PoolInfo
        {
            get => poolInfo;
            set
            {
                poolInfo = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PoolName)));
            }
        }

        private HitbloqProfile? HitbloqProfile
        {
            get => hitbloqProfile;
            set
            {
                hitbloqProfile = value;

                if (hitbloqProfile != null)
                {
                    if (modalProfilePic != null && modalTokenSource != null)
                    {
                        if (hitbloqProfile.ProfilePictureURL != null)
                        {
                            _ = spriteLoader.DownloadSpriteAsync(hitbloqProfile.ProfilePictureURL, sprite => modalProfilePic.sprite = sprite, modalTokenSource.Token);
                        }
                        else
                        {
                            _ = spriteLoader.FetchSpriteFromResourcesAsync("Hitbloq.Images.Logo.png", sprite => modalProfilePic.sprite = sprite, modalTokenSource.Token);
                        }   
                    }

                    if (hitbloqProfile.ProfileBackgroundURL != null && modalBackground != null && modalTokenSource != null)
                    {
                        _ = spriteLoader.DownloadSpriteAsync(hitbloqProfile.ProfileBackgroundURL, sprite =>
                        {
                            modalBackground.sprite = sprite;
                            modalBackground.color = customModalColour;
                            modalBackground.material = materialGrabber.NoGlowRoundEdge;
                        }, modalTokenSource.Token);
                    }
                }
            }
        }

        public HitbloqProfileModalController(IPlatformUserModel platformUserModel, UserIDSource userIDSource, ProfileSource profileSource, FriendIDSource friendIDSource,
            FriendsLeaderboardSource friendsLeaderboardSource, RankInfoSource rankInfoSource, PoolInfoSource poolInfoSource, SpriteLoader spriteLoader, MaterialGrabber materialGrabber)
        {
            this.platformUserModel = platformUserModel;
            this.userIDSource = userIDSource;
            this.profileSource = profileSource;
            this.friendIDSource = friendIDSource;
            this.friendsLeaderboardSource = friendsLeaderboardSource;
            this.rankInfoSource = rankInfoSource;
            this.poolInfoSource = poolInfoSource;
            this.spriteLoader = spriteLoader;
            this.materialGrabber = materialGrabber;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            parsed = true;
            modalView!.gameObject.name = "HitbloqProfileModal";

            modalBackground = modalView.transform.Find("BG").GetComponent<ImageView>();
            fogBG = modalBackground.material;
            roundRectSmall = modalBackground.sprite;
            originalModalColour = modalBackground.color;
            
            modalProfilePic!.material = materialGrabber.NoGlowRoundEdge;

            addFriendButton!.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
            addFriend = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.AddFriend.png");
            friendAdded = BeatSaberMarkupLanguage.Utilities.FindSpriteInAssembly("Hitbloq.Images.FriendAdded.png");

            if (modalInfoVertical!.background is ImageView verticalBackground)
            {
                verticalBackground.color = new Color(0f, 0f, 0f, 0.75f);
            }
        }

        private void Parse(Transform parentTransform)
        {
            if (!parsed)
            {
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqProfileModal.bsml"), parentTransform.gameObject, this);
                modalPosition = modalTransform!.localPosition;
            }
            modalTransform!.localPosition = modalPosition!.Value;

            modalProfilePic!.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
            modalBadge!.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;

            modalBackground!.sprite = roundRectSmall;
            modalBackground.color = originalModalColour!.Value;
            modalBackground.material = fogBG;

            Accessors.AnimateCanvasAccessor(ref modalView!) = true;
            Accessors.ViewValidAccessor(ref modalView!) = false;
        }

        internal void ShowModalForSelf(Transform parentTransform, HitbloqRankInfo rankInfo, string pool)
        {
            Parse(parentTransform);
            modalTokenSource?.Cancel();
            modalTokenSource?.Dispose();
            modalTokenSource = new CancellationTokenSource();
            _ = ShowModalForSelfAsync(modalTokenSource.Token, rankInfo, pool);
        }

        private async Task ShowModalForSelfAsync(CancellationToken cancellationToken, HitbloqRankInfo rankInfo, string pool)
        {
            await modalSemaphore.WaitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                var userID = await userIDSource.GetUserIDAsync();

                if (userID == null || userID.ID == -1)
                {
                    return;
                }

                await IPA.Utilities.Async.UnityMainThreadTaskScheduler.Factory.StartNew(() =>
                {
                    parserParams?.EmitEvent("close-modal");
                    parserParams?.EmitEvent("open-modal");
                });

                IsLoading = true;

                RankInfo = rankInfo;
                PoolInfo = await poolInfoSource.GetPoolInfoAsync(pool, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                
                HitbloqProfile = await profileSource.GetProfileAsync(userID.ID, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                
                addFriendButton!.gameObject.SetActive(false);
            }
            catch
            {
                // ignored
            }
            finally
            {
                IsLoading = false;
                modalSemaphore.Release();
            }
        }

        internal void ShowModalForUser(Transform parentTransform, int userID, string pool)
        {
            Parse(parentTransform);

            parserParams?.EmitEvent("close-modal");
            parserParams?.EmitEvent("open-modal");

            IsLoading = true;
            
            modalTokenSource?.Cancel();
            modalTokenSource?.Dispose();
            modalTokenSource = new CancellationTokenSource();

            _ = ShowModalForUserAsync(modalTokenSource.Token, userID, pool);
        }

        private async Task ShowModalForUserAsync(CancellationToken cancellationToken, int userID, string pool)
        {
            await modalSemaphore.WaitAsync(cancellationToken);
            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            try
            {
                RankInfo = await rankInfoSource.GetRankInfoAsync(pool, userID, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                
                PoolInfo = await poolInfoSource.GetPoolInfoAsync(pool, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                
                HitbloqProfile = await profileSource.GetProfileAsync(userID, cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }

                var selfID = await userIDSource.GetUserIDAsync();
                addFriendButton!.gameObject.SetActive(selfID?.ID != userID);
                var platformFriends = await friendIDSource.GetPlatformFriendIDsAsync(cancellationToken);
                
                if (cancellationToken.IsCancellationRequested)
                {
                    return;
                }
                
                if (platformFriends != null && platformFriends.Contains(userID))
                {
                    addFriendButton.image.sprite = friendAdded;
                    AddFriendInteractable = false;
                    AddFriendHoverHint = kAlreadyFriendPrompt + (await platformUserModel.GetUserInfo()).platform;
                }
                else
                {
                    this.userID = userID;
                    AddFriendInteractable = true;
                    IsFriend = PluginConfig.Instance.Friends.Contains(userID);
                }
            }
            catch
            {
                // ignored
            }
            finally
            {
                IsLoading = false;
                modalSemaphore.Release();
            }
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
            get => isLoading;
            set
            {
                isLoading = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotLoading)));
            }
        }

        [UIValue("is-not-loading")]
        private bool IsNotLoading => !isLoading;

        [UIValue("add-friend-interactable")]
        private bool AddFriendInteractable
        {
            get => addFriendInteractable;
            set
            {
                addFriendInteractable = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AddFriendInteractable)));
            }
        }

        [UIValue("add-friend-hover")]
        private string AddFriendHoverHint
        {
            get => addFriendHoverHint;
            set
            {
                addFriendHoverHint = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(AddFriendHoverHint)));
            }
        }

        [UIValue("username")]
        private string Username
        {
            get
            {
                var userName = $"{RankInfo?.Username}";
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
                var poolName = $"{PoolInfo?.ShownName}".RemoveSpecialCharacters();
                if (poolName.DoesNotHaveAlphaNumericCharacters())
                {
                    poolName = $"{PoolInfo?.ID}";
                }
                if (poolName.Length > 16)
                {
                    return $"{poolName.Substring(0, 13)}...";
                }
                return poolName;
            }
        }

        [UIValue("rank")]
        private string Rank => $"#{RankInfo?.Rank}";

        [UIValue("cr")]
        private string CR => $"{RankInfo?.CR}";

        [UIValue("score-count")]
        private string ScoreCount => $"{RankInfo?.ScoreCount}";
    }
}
