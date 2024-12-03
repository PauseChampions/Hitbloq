using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using Hitbloq.Entries;
using Hitbloq.Other;
using Hitbloq.Sources;
using Hitbloq.Utilities;
using HMUI;
using TMPro;
using Tweening;
using UnityEngine;

namespace Hitbloq.UI.ViewControllers
{
	internal class HitbloqPoolLeaderboardCellController : TableCell, INotifyPropertyChanged
	{
		[UIComponent("cr-text")]
		private readonly TextMeshProUGUI _crText = null!;

		[UIComponent("profile-picture")]
		private readonly ImageView _profilePicture = null!;

		[UIComponent("rank-change-text")]
		private readonly TextMeshProUGUI _rankChangeText = null!;

		[UIComponent("rank-text")]
		private readonly TextMeshProUGUI _rankText = null!;

		[UIComponent("username-text")]
		private readonly TextMeshProUGUI _usernameText = null!;

		private bool _isSelf;
		private MaterialGrabber _materialGrabber = null!;
		private HitbloqPoolLeaderboardEntry? _poolLeaderboardEntry;
		private SpriteLoader _spriteLoader = null!;

		private UserIDSource _userIDSource = null!;
		private TweeningManager _uwuTweenyManager = null!;

		[UIValue("rank")]
		private string Rank => _poolLeaderboardEntry?.Rank.ToString() ?? "";

		[UIValue("username")]
		private string Username => _poolLeaderboardEntry?.Username ?? "";

		[UIValue("cr")]
		private string CR => $"{_poolLeaderboardEntry?.CR}cr";

		[UIValue("rank-change")]
		private string RankChange => _poolLeaderboardEntry?.RankChange.ToString() ?? "";

		[UIAction("#post-parse")]
		private void PostParse()
		{
			_profilePicture.material = _materialGrabber.NoGlowRoundEdge;

			_fogBg = _background.Background.material;
			_roundRectSmall = _background.Background.sprite;
			_originalBackgroundColour = _background.Background.color;
		}

		public void SetRequiredUtils(UserIDSource userIDSource, SpriteLoader spriteLoader, MaterialGrabber materialGrabber, TweeningManager uwuTweenyManager)
		{
			_userIDSource = userIDSource;
			_spriteLoader = spriteLoader;
			_materialGrabber = materialGrabber;
			_uwuTweenyManager = uwuTweenyManager;
		}

		public HitbloqPoolLeaderboardCellController PopulateCell(HitbloqPoolLeaderboardEntry poolLeaderboardEntry)
		{
			if (poolLeaderboardEntry == _poolLeaderboardEntry)
			{
				return this;
			}

			_poolLeaderboardEntry = poolLeaderboardEntry;
			_ = CheckIfSelf();
			_ = FetchProfilePicture();
			_ = FetchBackground();

			NotifyPropertyChanged(nameof(Rank));
			NotifyPropertyChanged(nameof(Username));
			NotifyPropertyChanged(nameof(CR));
			NotifyPropertyChanged(nameof(RankChange));

			if (poolLeaderboardEntry.RankChange > 0)
			{
				_rankChangeText.color = Color.green;
			}
			else if (poolLeaderboardEntry.RankChange < 0)
			{
				_rankChangeText.color = Color.red;
			}
			else
			{
				_rankChangeText.color = Color.white;
			}

			return this;
		}

		private async Task CheckIfSelf()
		{
			_isSelf = false;

			var userID = await _userIDSource.GetUserIDAsync();
			if (userID != null && userID.ID == _poolLeaderboardEntry!.UserID)
			{
				_isSelf = true;
				RefreshBackground();
			}
		}

		private async Task FetchProfilePicture()
		{
			var currentEntry = _poolLeaderboardEntry;
			_profilePicture.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;

			if (_poolLeaderboardEntry is not {ProfilePictureURL: { }})
			{
				await _spriteLoader.FetchSpriteFromResourcesAsync("Hitbloq.Images.Logo.png", sprite =>
				{
					if (_poolLeaderboardEntry == currentEntry)
					{
						_profilePicture.sprite = sprite;
					}
				});
				return;
			}

			await _spriteLoader.DownloadSpriteAsync(_poolLeaderboardEntry.ProfilePictureURL, sprite =>
			{
				if (_poolLeaderboardEntry == currentEntry)
				{
					_profilePicture.sprite = sprite;
				}
			});
		}

		#region Background

		private Sprite? _roundRectSmall;
		private Material? _fogBg;

		private Color? _originalBackgroundColour;
		private readonly Color _customBackgroundColour = new(0.75f, 0.75f, 0.75f, 1f);

		[UIComponent("background")]
		private readonly Backgroundable _background = null!;

		private async Task FetchBackground()
		{
			if (_background.Background is not ImageView bgImageView)
			{
				return;
			}

			var currentEntry = _poolLeaderboardEntry;

			bgImageView.sprite = _roundRectSmall;
			bgImageView.overrideSprite = _roundRectSmall;
			bgImageView.color = _originalBackgroundColour!.Value;
			bgImageView.material = _fogBg;
			Accessors.GradientAccessor(ref bgImageView) = false;

			if (_poolLeaderboardEntry is not {BannerImageURL: { }})
			{
				return;
			}

			await _spriteLoader.DownloadSpriteAsync(_poolLeaderboardEntry.BannerImageURL, sprite =>
			{
				if (_poolLeaderboardEntry == currentEntry)
				{
					bgImageView.sprite = sprite;
					bgImageView.overrideSprite = sprite;
					bgImageView.color = _customBackgroundColour;
					bgImageView.color0 = new Color(0.5f, 0.5f, 0.5f, 1f);
					bgImageView.color1 = Color.white;
					Accessors.GradientAccessor(ref bgImageView) = true;
					bgImageView.material = _materialGrabber.NoGlowRoundEdge;
				}
			});
		}

		#endregion

		#region Highlight and Selection

		private readonly Color _backgroundHighlightedColor = new(0.5f, 0.5f, 0.5f, 1f);
		private readonly Color _textColor = new(1, 1, 1, 0.7490196f);
		private readonly Color _selfTextColor = new(0, 0.7529412f, 1, 0.7490196f);
		private readonly Color _selfTextHighlightedColor = new(0, 0.7529412f, 1, 1);
		private readonly Color _crColor = new(0.7254902f, 0.5294118f, 1, 0.7490196f);
		private readonly Color _crHighlightedColor = new(0.7254902f, 0.5294118f, 1, 1f);

		public override void SelectionDidChange(TransitionType transitionType)
		{
			RefreshBackground();
		}

		public override void HighlightDidChange(TransitionType transitionType)
		{
			RefreshBackground();
		}

		private void RefreshBackground()
		{
			if (!interactable)
			{
				return;
			}

			_uwuTweenyManager.KillAllTweens(this);

			if (highlighted)
			{
				var currentColor = _background.Background.color;

				var tween = new FloatTween(0, 1, val => { _background.Background.color = Color.Lerp(currentColor, _backgroundHighlightedColor, val); }, 0.25f, EaseType.Linear);

				_uwuTweenyManager.AddTween(tween, this);

				_rankText.color = _isSelf ? _selfTextHighlightedColor : Color.white;
				_usernameText.color = _isSelf ? _selfTextHighlightedColor : Color.white;
				_crText.color = _crHighlightedColor;
			}
			else
			{
				if (_poolLeaderboardEntry is {BannerImageURL: { }})
				{
					_background.Background.color = _customBackgroundColour;
				}
				else
				{
					_background.Background.color = _originalBackgroundColour!.Value;
				}

				_rankText.color = _isSelf ? _selfTextColor : _textColor;
				_usernameText.color = _isSelf ? _selfTextColor : _textColor;
				_crText.color = _crColor;
			}
		}

		#endregion

		#region Property Changed

		public event PropertyChangedEventHandler? PropertyChanged;

		private void NotifyPropertyChanged([CallerMemberName] string? propertyName = null)
		{
			PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
		}

		#endregion
	}
}