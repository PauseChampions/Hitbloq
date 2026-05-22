using System;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage.Attributes;
using Hitbloq.Other;
using HMUI;
using IPA.Utilities.Async;
using UnityEngine;

namespace Hitbloq.UI.ViewControllers
{
	internal sealed class HitbloqLeaderboardProfilePictureView
	{
		private readonly Sprite _blankSprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
		private const int ProfilePictureTimeoutMilliseconds = 10000;

		private MaterialGrabber? _materialGrabber;
		private SpriteLoader? _spriteLoader;
		private int _loadVersion;

		[UIComponent("profile-image")]
		private readonly ImageView _profileImage = null!;

		[UIObject("profile-loading")]
		private readonly GameObject _loadingIndicator = null!;

		public void SetRequiredUtils(SpriteLoader spriteLoader, MaterialGrabber materialGrabber)
		{
			_spriteLoader = spriteLoader;
			_materialGrabber = materialGrabber;

			if (_profileImage != null)
			{
				_profileImage.material = _materialGrabber.NoGlowRoundEdge;
			}
		}

		[UIAction("#post-parse")]
		private void PostParse()
		{
			_profileImage.sprite = _blankSprite;
			_profileImage.gameObject.SetActive(true);
			_loadingIndicator.SetActive(false);
		}

		public void ClearSprite()
		{
			_loadVersion++;
			ClearSpriteOnMainThread();
		}

		public void SetProfilePicture(string profilePictureURL, CancellationToken cancellationToken)
		{
			if (_spriteLoader == null || string.IsNullOrWhiteSpace(profilePictureURL))
			{
				ClearSprite();
				return;
			}

			var loadVersion = ++_loadVersion;
			var timeoutTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
			timeoutTokenSource.CancelAfter(ProfilePictureTimeoutMilliseconds);

			SetLoadingActive(true);
			_ = HideLoadingAfterTimeout(loadVersion, timeoutTokenSource);
			_ = DownloadSpriteAsync(profilePictureURL, loadVersion, timeoutTokenSource);
		}

		private async Task DownloadSpriteAsync(string profilePictureURL, int loadVersion, CancellationTokenSource timeoutTokenSource)
		{
			try
			{
				await _spriteLoader!.DownloadSpriteAsync(profilePictureURL, sprite =>
				{
					_ = ApplySpriteAsync(sprite, loadVersion, timeoutTokenSource.Token);
				}, timeoutTokenSource.Token);
			}
			catch (Exception)
			{
				await HideLoadingAsync(loadVersion, timeoutTokenSource.Token);
			}
		}

		private async Task ApplySpriteAsync(Sprite? sprite, int loadVersion, CancellationToken cancellationToken)
		{
			if (cancellationToken.IsCancellationRequested || loadVersion != _loadVersion)
			{
				return;
			}

			await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
			{
				if (cancellationToken.IsCancellationRequested || loadVersion != _loadVersion || _profileImage == null || _loadingIndicator == null)
				{
					return;
				}

				_profileImage.sprite = sprite ?? _blankSprite;
				_loadingIndicator.SetActive(false);
			});
		}

		private async Task HideLoadingAfterTimeout(int loadVersion, CancellationTokenSource timeoutTokenSource)
		{
			try
			{
				await Task.Delay(ProfilePictureTimeoutMilliseconds, timeoutTokenSource.Token);
				timeoutTokenSource.Cancel();
			}
			catch (TaskCanceledException)
			{
			}

			await HideLoadingAsync(loadVersion, timeoutTokenSource.Token, allowCancelledToken: true);
		}

		private async Task HideLoadingAsync(int loadVersion, CancellationToken cancellationToken, bool allowCancelledToken = false)
		{
			if ((!allowCancelledToken && cancellationToken.IsCancellationRequested) || loadVersion != _loadVersion || _loadingIndicator == null)
			{
				return;
			}

			await UnityMainThreadTaskScheduler.Factory.StartNew(() =>
			{
				if ((!allowCancelledToken && cancellationToken.IsCancellationRequested) || loadVersion != _loadVersion || _loadingIndicator == null)
				{
					return;
				}

				_loadingIndicator.SetActive(false);
			});
		}

		private void ClearSpriteOnMainThread()
		{
			if (_profileImage == null || _loadingIndicator == null)
			{
				return;
			}

			_profileImage.sprite = _blankSprite;
			_loadingIndicator.SetActive(false);
		}

		private void SetLoadingActive(bool active)
		{
			if (_loadingIndicator != null)
			{
				_loadingIndicator.SetActive(active);
			}
		}
	}
}
