using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using Hitbloq.Entries;
using Hitbloq.Other;
using HMUI;
using TMPro;
using Tweening;
using UnityEngine;

namespace Hitbloq.UI
{
	internal class HitbloqPoolCellController : TableCell, INotifyPropertyChanged
	{
		[UIComponent("banner-image")]
		private readonly ImageView _bannerImage = null!;

		[UIComponent("description-text")]
		private readonly TextMeshProUGUI _descriptionText = null!;

		[UIComponent("player-count-text")]
		private readonly TextMeshProUGUI _playerCountText = null!;

		[UIComponent("pool-name-text")]
		private readonly TextMeshProUGUI _poolNameText = null!;

		[UIComponent("popularity-text")]
		private readonly TextMeshProUGUI _popularityText = null!;

		private MaterialGrabber _materialGrabber = null!;
		private HitbloqPoolListEntry? _poolListEntry;
		private bool _spriteDownloaded;

		private SpriteLoader _spriteLoader = null!;
		private TweeningManager _uwuTweenyManager = null!;

		[UIValue("pool-name")]
		private string PoolName => $"{_poolListEntry?.Title}";

		[UIValue("show-banner-title")]
		private bool ShowBannerTitle => (!_poolListEntry?.BannerTitleHide ?? true) || !_spriteDownloaded || (interactable && (highlighted || selected));

		[UIValue("description")]
		private string Description => $"{_poolListEntry?.ShortDescription}";

		[UIValue("popularity")]
		private string Popularity => $"📈 {_poolListEntry?.Popularity}";

		[UIValue("player-count")]
		private string PlayerCount => $"👥 {_poolListEntry?.PlayerCount}";

		[UIAction("#post-parse")]
		private void PostParse()
		{
			_bannerImage.material = _materialGrabber.NoGlowRoundEdge;
		}

		public void SetRequiredUtils(SpriteLoader spriteLoader, MaterialGrabber materialGrabber, TweeningManager uwuTweenyManager)
		{
			_spriteLoader = spriteLoader;
			_materialGrabber = materialGrabber;
			_uwuTweenyManager = uwuTweenyManager;
		}

		public HitbloqPoolCellController PopulateCell(HitbloqPoolListEntry poolListEntry)
		{
			_poolListEntry = poolListEntry;
			_ = FetchBanner();

			NotifyPropertyChanged(nameof(PoolName));
			NotifyPropertyChanged(nameof(Description));
			NotifyPropertyChanged(nameof(Popularity));
			NotifyPropertyChanged(nameof(PlayerCount));

			_poolNameText.alpha = 1;
			return this;
		}

		private async Task FetchBanner()
		{
			_uwuTweenyManager.KillAllTweens(this);
			_spriteDownloaded = false;
			NotifyPropertyChanged(nameof(ShowBannerTitle));
			var currentEntry = _poolListEntry;

			await _spriteLoader.FetchSpriteFromResourcesAsync("Hitbloq.Images.DefaultPoolBanner.png", sprite =>
			{
				if (_poolListEntry == currentEntry)
				{
					_bannerImage.sprite = sprite;
				}
			});

			if (_poolListEntry is not {BannerImageURL: { }})
			{
				return;
			}

			await _spriteLoader.DownloadSpriteAsync(_poolListEntry.BannerImageURL, sprite =>
			{
				if (_poolListEntry == currentEntry)
				{
					_bannerImage.color = new Color(1, 1, 1, 0);

					_bannerImage.sprite = sprite;
					_spriteDownloaded = true;
					NotifyPropertyChanged(nameof(ShowBannerTitle));

					if (!selected && !highlighted)
					{
						var tween = new FloatTween(0, 1, val => { _bannerImage.color = new Color(1, 1, 1, val); }, 0.5f, EaseType.Linear);

						_uwuTweenyManager.AddTween(tween, this);
					}
					else
					{
						RefreshBackground();
					}
				}
			});
		}

		#region Highlight and Selection

		private readonly Color _selectedColor = new(0.25f, 0.25f, 0.25f, 1);
		private readonly Color _textSelectedColor = new(0, 0.7529412f, 1, 1);
		private readonly Color _textColor = new(1, 1, 1, 0.7490196f);

		protected override void SelectionDidChange(TransitionType transitionType)
		{
			RefreshBackground();
		}

		protected override void HighlightDidChange(TransitionType transitionType)
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
			if (selected)
			{
				_poolNameText.alpha = 1;
				_bannerImage.color = _selectedColor;
				_descriptionText.color = _textSelectedColor;
				_popularityText.color = _textSelectedColor;
				_playerCountText.color = _textSelectedColor;
			}
			else if (highlighted)
			{
				var currentColor = _bannerImage.color;

				if (_poolListEntry?.BannerTitleHide ?? false)
				{
					_poolNameText.alpha = 0;
				}

				var tween = new FloatTween(0, 1, val =>
				{
					_bannerImage.color = Color.Lerp(currentColor, Color.gray, val);
					if (_poolListEntry?.BannerTitleHide ?? false)
					{
						_poolNameText.alpha = val;
					}
				}, 0.25f, EaseType.Linear);

				_uwuTweenyManager.AddTween(tween, this);
				_descriptionText.color = Color.white;
				_popularityText.color = Color.white;
				_playerCountText.color = Color.white;
			}
			else
			{
				_bannerImage.color = Color.white;
				_descriptionText.color = _textColor;
				_popularityText.color = _textColor;
				_playerCountText.color = _textColor;
			}

			NotifyPropertyChanged(nameof(ShowBannerTitle));
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