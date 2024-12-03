using System.ComponentModel;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
using IPA.Utilities.Async;
using UnityEngine;

namespace Hitbloq.UI.ViewControllers
{
	internal class HitbloqProfileModalController : INotifyPropertyChanged
	{
		private const string KAddFriendPrompt = "Add to friends list";
		private const string KRemoveFriendPrompt = "Remove from friends list";
		private const string KAlreadyFriendPrompt = "You are already friends on ";

		[UIComponent("add-friend")]
		private readonly ButtonIconImage? _addFriendButton = null!;

		private readonly Color _customModalColour = new(0.5f, 0.5f, 0.5f, 1f);
		private readonly FriendIDSource _friendIDSource;
		private readonly FriendsLeaderboardSource _friendsLeaderboardSource;
		private readonly MaterialGrabber _materialGrabber;

		[UIComponent("modal-badge")]
		private readonly ImageView? _modalBadge = null!;

		[UIComponent("modal-info-vertical")]
		private readonly Backgroundable? _modalInfoVertical = null!;

		[UIComponent("modal-profile-pic")]
		private readonly ImageView? _modalProfilePic = null!;

		private readonly SemaphoreSlim _modalSemaphore = new(1, 1);

		[UIComponent("modal")]
		private readonly RectTransform? _modalTransform = null!;

		[UIParams]
		private readonly BSMLParserParams? _parserParams = null!;

		private readonly IPlatformUserModel _platformUserModel;
		private readonly PoolInfoSource _poolInfoSource;
		private readonly ProfileSource _profileSource;
		private readonly RankInfoSource _rankInfoSource;
		private readonly SpriteLoader _spriteLoader;
		private readonly UserIDSource _userIDSource;
		private Sprite? _addFriend;
		private string _addFriendHoverHint = "";
		private bool _addFriendInteractable;

		private Material? _fogBg;
		private Sprite? _friendAdded;
		private HitbloqProfile? _hitbloqProfile;

		private bool _isFriend;
		private bool _isLoading;
		private ImageView? _modalBackground;

		private Vector3? _modalPosition;
		private CancellationTokenSource? _modalTokenSource;

		[UIComponent("modal")]
		private ModalView? _modalView;

		private Color? _originalModalColour;

		private bool _parsed;
		private HitbloqPoolInfo? _poolInfo;
		private HitbloqRankInfo? _rankInfo;
		private Sprite? _roundRectSmall;

		private int _userID;

		public HitbloqProfileModalController(IPlatformUserModel platformUserModel, UserIDSource userIDSource, ProfileSource profileSource, FriendIDSource friendIDSource, FriendsLeaderboardSource friendsLeaderboardSource, RankInfoSource rankInfoSource, PoolInfoSource poolInfoSource, SpriteLoader spriteLoader, MaterialGrabber materialGrabber)
		{
			_platformUserModel = platformUserModel;
			_userIDSource = userIDSource;
			_profileSource = profileSource;
			_friendIDSource = friendIDSource;
			_friendsLeaderboardSource = friendsLeaderboardSource;
			_rankInfoSource = rankInfoSource;
			_poolInfoSource = poolInfoSource;
			_spriteLoader = spriteLoader;
			_materialGrabber = materialGrabber;
		}

		private bool IsFriend
		{
			get => _isFriend;
			set
			{
				_isFriend = value;

				if (_addFriendButton != null)
				{
					if (value)
					{
						_addFriendButton.Image.sprite = _friendAdded;
						AddFriendHoverHint = KRemoveFriendPrompt;
					}
					else
					{
						_addFriendButton.Image.sprite = _addFriend;
						AddFriendHoverHint = KAddFriendPrompt;
					}
				}
			}
		}

		private HitbloqRankInfo? RankInfo
		{
			get => _rankInfo;
			set
			{
				_rankInfo = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Username)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Rank)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CR)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(ScoreCount)));

				if (_modalBadge != null && _rankInfo != null && _modalTokenSource != null)
				{
					_ = _spriteLoader.DownloadSpriteAsync(_rankInfo.TierURL, sprite => _modalBadge.sprite = sprite, _modalTokenSource.Token);
				}
			}
		}

		private HitbloqPoolInfo? PoolInfo
		{
			get => _poolInfo;
			set
			{
				_poolInfo = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(PoolName)));
			}
		}

		private HitbloqProfile? HitbloqProfile
		{
			get => _hitbloqProfile;
			set
			{
				_hitbloqProfile = value;

				if (_hitbloqProfile != null)
				{
					if (_modalProfilePic != null && _modalTokenSource != null)
					{
						if (_hitbloqProfile.ProfilePictureURL != null)
						{
							_ = _modalProfilePic.SetImageAsync(_hitbloqProfile.ProfilePictureURL);
						}
						else
						{
							_ = _spriteLoader.FetchSpriteFromResourcesAsync("Hitbloq.Images.Logo.png", sprite => _modalProfilePic.sprite = sprite, _modalTokenSource.Token);
						}
					}

					if (_hitbloqProfile.ProfileBackgroundURL != null && _modalBackground != null && _modalTokenSource != null)
					{
						_ = _spriteLoader.DownloadSpriteAsync(_hitbloqProfile.ProfileBackgroundURL, sprite =>
						{
							_modalBackground.sprite = sprite;
							_modalBackground.color = _customModalColour;
							_modalBackground.material = _materialGrabber.NoGlowRoundEdge;
						}, _modalTokenSource.Token);
					}
				}
			}
		}

		[UIValue("is-loading")]
		private bool IsLoading
		{
			get => _isLoading;
			set
			{
				_isLoading = value;
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsLoading)));
				PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(IsNotLoading)));
			}
		}

		[UIValue("is-not-loading")]
		private bool IsNotLoading => !_isLoading;

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

		public event PropertyChangedEventHandler? PropertyChanged;

		[UIAction("#post-parse")]
		private async Task PostParse()
		{
			_parsed = true;
			_modalView!.gameObject.name = "HitbloqProfileModal";

			_modalBackground = _modalView.transform.Find("BG").GetComponent<ImageView>();
			_fogBg = _modalBackground.material;
			_roundRectSmall = _modalBackground.sprite;
			_originalModalColour = _modalBackground.color;

			_modalProfilePic!.material = _materialGrabber.NoGlowRoundEdge;

			_addFriendButton!.transform.localScale = new Vector3(0.3f, 0.3f, 1f);
			_addFriend = await BeatSaberMarkupLanguage.Utilities.LoadSpriteFromAssemblyAsync("Hitbloq.Images.AddFriend.png");
			_friendAdded = await BeatSaberMarkupLanguage.Utilities.LoadSpriteFromAssemblyAsync("Hitbloq.Images.FriendAdded.png");

			if (_modalInfoVertical!.Background is ImageView verticalBackground)
			{
				verticalBackground.color = new Color(0f, 0f, 0f, 0.75f);
			}
		}

		private void Parse(Transform parentTransform)
		{
			if (!_parsed)
			{
				BSMLParser.Instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqProfileModal.bsml"), parentTransform.gameObject, this);
				_modalPosition = _modalTransform!.localPosition;
			}

			_modalTransform!.localPosition = _modalPosition!.Value;

			_modalProfilePic!.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
			_modalBadge!.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;

			_modalBackground!.sprite = _roundRectSmall;
			_modalBackground.color = _originalModalColour!.Value;
			_modalBackground.material = _fogBg;

			Accessors.AnimateCanvasAccessor(ref _modalView!) = true;
			Accessors.ViewValidAccessor(ref _modalView!) = false;
		}

		internal void ShowModalForSelf(Transform parentTransform, HitbloqRankInfo rankInfo, string pool)
		{
			Parse(parentTransform);
			_modalTokenSource?.Cancel();
			_modalTokenSource?.Dispose();
			_modalTokenSource = new CancellationTokenSource();
			_ = ShowModalForSelfAsync(_modalTokenSource.Token, rankInfo, pool);
		}

		private async Task ShowModalForSelfAsync(CancellationToken cancellationToken, HitbloqRankInfo rankInfo, string pool)
		{
			await _modalSemaphore.WaitAsync(cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			try
			{
				var userID = await _userIDSource.GetUserIDAsync();

				if (userID == null || userID.ID == -1)
				{
					return;
				}

				await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
				{
					_parserParams?.EmitEvent("close-modal");
					_parserParams?.EmitEvent("open-modal");
				});

				IsLoading = true;

				RankInfo = rankInfo;
				PoolInfo = await _poolInfoSource.GetPoolInfoAsync(pool, cancellationToken);

				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				HitbloqProfile = await _profileSource.GetProfileAsync(userID.ID, cancellationToken);

				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				_addFriendButton!.gameObject.SetActive(false);
			}
			catch
			{
				// ignored
			}
			finally
			{
				IsLoading = false;
				_modalSemaphore.Release();
			}
		}

		internal void ShowModalForUser(Transform parentTransform, int userID, string pool)
		{
			Parse(parentTransform);

			_parserParams?.EmitEvent("close-modal");
			_parserParams?.EmitEvent("open-modal");

			IsLoading = true;

			_modalTokenSource?.Cancel();
			_modalTokenSource?.Dispose();
			_modalTokenSource = new CancellationTokenSource();

			_ = ShowModalForUserAsync(_modalTokenSource.Token, userID, pool);
		}

		private async Task ShowModalForUserAsync(CancellationToken cancellationToken, int userID, string pool)
		{
			await _modalSemaphore.WaitAsync(cancellationToken);
			if (cancellationToken.IsCancellationRequested)
			{
				return;
			}

			try
			{
				RankInfo = await _rankInfoSource.GetRankInfoAsync(pool, userID, cancellationToken);

				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				PoolInfo = await _poolInfoSource.GetPoolInfoAsync(pool, cancellationToken);

				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				HitbloqProfile = await _profileSource.GetProfileAsync(userID, cancellationToken);

				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				var selfID = await _userIDSource.GetUserIDAsync();
				_addFriendButton!.gameObject.SetActive(selfID?.ID != userID);
				var platformFriends = await _friendIDSource.GetPlatformFriendIDsAsync(cancellationToken);

				if (cancellationToken.IsCancellationRequested)
				{
					return;
				}

				if (platformFriends != null && platformFriends.Contains(userID))
				{
					_addFriendButton.Image.sprite = _friendAdded;
					AddFriendInteractable = false;
					AddFriendHoverHint = KAlreadyFriendPrompt + (await _platformUserModel.GetUserInfo(CancellationToken.None)).platform;
				}
				else
				{
					_userID = userID;
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
				_modalSemaphore.Release();
			}
		}

		[UIAction("add-friend-click")]
		private void AddFriendClicked()
		{
			if (IsFriend)
			{
				PluginConfig.Instance.Friends.Remove(_userID);
				PluginConfig.Instance.Changed();
			}
			else
			{
				PluginConfig.Instance.Friends.Add(_userID);
				PluginConfig.Instance.Changed();
			}

			IsFriend = !IsFriend;
			_friendsLeaderboardSource.ClearCache();
		}
	}
}