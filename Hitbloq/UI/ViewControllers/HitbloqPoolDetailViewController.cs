using System;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.ViewControllers;
using Hitbloq.Entries;
using Hitbloq.Other;
using HMUI;
using Tweening;
using UnityEngine;
using Zenject;

namespace Hitbloq.UI.ViewControllers
{
	[HotReload(RelativePathToLayout = @"..\Views\HitbloqPoolDetailView.bsml")]
	[ViewDefinition("Hitbloq.UI.Views.HitbloqPoolDetailView.bsml")]
	internal class HitbloqPoolDetailViewController : BSMLAutomaticViewController
	{
		[UIComponent("text-page")]
		private readonly TextPageScrollView _descriptionTextPage = null!;

		[Inject]
		private readonly MaterialGrabber _materialGrabber = null!;

		[InjectOptional]
		private readonly PlaylistManagerIHardlyKnowHer? _playlistManagerIHardlyKnowHer = null!;

		[UIComponent("pool-cell")]
		private readonly RectTransform? _poolCellParentTransform = null!;

		[Inject]
		private readonly SpriteLoader _spriteLoader = null!;

		[Inject]
		private readonly TimeTweeningManager _uwuTweenyManager = null!;

		private HitbloqPoolCellController? _hitbloqPoolCell;
		private HitbloqPoolListEntry? _hitbloqPoolListEntry;
		private BeatmapLevelPack? _localPlaylist;
		private CancellationTokenSource? _playlistSearchTokenSource;

		public event Action? FlowDismissRequested;

		#region Actions

		[UIAction("#post-parse")]
		private void PostParse()
		{
			rectTransform.anchorMax = new Vector2(0.5f, 1);
			_hitbloqPoolCell = new GameObject("PoolCellDetail").AddComponent<HitbloqPoolCellController>();
			_hitbloqPoolCell.transform.SetParent(_poolCellParentTransform, false);
			_hitbloqPoolCell.transform.SetSiblingIndex(0);
			_hitbloqPoolCell.SetRequiredUtils(_spriteLoader, _materialGrabber, _uwuTweenyManager);
			_hitbloqPoolCell.interactable = false;
			BSMLParser.Instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqPoolCell.bsml"), _hitbloqPoolCell.gameObject, _hitbloqPoolCell);
		}

		[UIAction("download-click")]
		private void DownloadPressed()
		{
			_ = DownloadPoolAsync();
		}

		private async Task DownloadPoolAsync()
		{
			var pool = _hitbloqPoolListEntry;
			if (pool != null && _playlistManagerIHardlyKnowHer != null)
			{
				pool.DownloadBlocked = true;
				NotifyPropertyChanged(nameof(DownloadInteractable));

				var localPlaylist = await _playlistManagerIHardlyKnowHer.DownloadPlaylistFromPoolID(pool.ID);
				if (_hitbloqPoolListEntry == pool)
				{
					_localPlaylist = localPlaylist;
				}

				pool.DownloadBlocked = false;
				NotifyPropertyChanged(nameof(DownloadInteractable));
				NotifyPropertyChanged(nameof(DownloadActive));
				NotifyPropertyChanged(nameof(GoToActive));
			}
		}

		[UIAction("go-to-playlist")]
		private void GoToPlaylist()
		{
			if (_playlistManagerIHardlyKnowHer != null && _localPlaylist != null)
			{
				FlowDismissRequested?.Invoke();
				_playlistManagerIHardlyKnowHer.OpenPlaylist(_localPlaylist);
			}
		}

		public void SetPool(HitbloqPoolListEntry hitbloqPoolListEntry)
		{
			_hitbloqPoolListEntry = hitbloqPoolListEntry;
			_localPlaylist = null;

			if (_hitbloqPoolCell != null)
			{
				_hitbloqPoolCell.PopulateCell(hitbloqPoolListEntry);
			}

			NotifyPropertyChanged(nameof(Description));
			NotifyPropertyChanged(nameof(DownloadInteractable));
			NotifyPropertyChanged(nameof(DownloadActive));
			NotifyPropertyChanged(nameof(GoToActive));

			_descriptionTextPage.ScrollTo(0, true);

			_playlistSearchTokenSource?.Cancel();
			_playlistSearchTokenSource?.Dispose();
			_playlistSearchTokenSource = new CancellationTokenSource();
			_ = FetchPlaylistAsync(hitbloqPoolListEntry.ID, _playlistSearchTokenSource.Token);
		}

		private async Task FetchPlaylistAsync(string poolID, CancellationToken token)
		{
			if (_playlistManagerIHardlyKnowHer == null)
			{
				return;
			}

			var localPlaylist = await _playlistManagerIHardlyKnowHer.FindLocalPlaylistFromPoolID(poolID, token);

			if (localPlaylist != null)
			{
				_localPlaylist = localPlaylist;
				NotifyPropertyChanged(nameof(DownloadActive));
				NotifyPropertyChanged(nameof(GoToActive));
			}
		}

		#endregion

		#region Values

		[UIValue("description")]
		private string Description => $"Owners: {_hitbloqPoolListEntry?.Author}\n\n" + (string.IsNullOrWhiteSpace(_hitbloqPoolListEntry?.Description) ? "No Description available for this pool." : _hitbloqPoolListEntry?.Description ?? "");

		[UIValue("download-interactable")]
		public bool DownloadInteractable => _hitbloqPoolListEntry is {DownloadBlocked: false};

		[UIValue("download-active")]
		public bool DownloadActive => _playlistManagerIHardlyKnowHer is {CanOpenPlaylist: true} && _hitbloqPoolListEntry != null && _localPlaylist == null;

		[UIValue("go-to-active")]
		public bool GoToActive => _playlistManagerIHardlyKnowHer != null && _localPlaylist != null;

		#endregion
	}
}