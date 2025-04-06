using System.Reflection;
using System.Threading.Tasks;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Configuration;
using Hitbloq.Entries;
using Hitbloq.Interfaces;
using Hitbloq.Other;
using Hitbloq.Utilities;
using HMUI;
using IPA.Utilities.Async;
using UnityEngine;

namespace Hitbloq.UI.ViewControllers
{
	internal class HitbloqEventModalViewController : NotifiableBase, INotifyViewActivated
	{
		[UIComponent("text-page")]
		private readonly TextPageScrollView? _descriptionTextPage = null!;

		[UIComponent("event-image")]
		private readonly ImageView? _eventImage = null!;

		private readonly IEventSource _eventSource;
		private readonly HitbloqFlowCoordinator _hitbloqFlowCoordinator;

		[UIComponent("modal")]
		private readonly RectTransform? _modalTransform = null!;

		[UIParams]
		private readonly BSMLParserParams? _parserParams = null!;

		private readonly SpriteLoader _spriteLoader;

		private HitbloqEvent? _currentEvent;

		private Vector3? _modalPosition;

		[UIComponent("modal")]
		private ModalView? _modalView;

		private bool _parsed;

		public HitbloqEventModalViewController(IEventSource eventSource, SpriteLoader spriteLoader, HitbloqFlowCoordinator hitbloqFlowCoordinator)
		{
			_eventSource = eventSource;
			_spriteLoader = spriteLoader;
			_hitbloqFlowCoordinator = hitbloqFlowCoordinator;
		}

		[UIValue("event-title")]
		private string EventTitle => $"{_currentEvent?.Title}";

		[UIValue("event-description")]
		private string EventDescription => $"{_currentEvent?.Description}";

		[UIValue("pool-exists")]
		private bool PoolExists => _currentEvent is {Pool: { }};

		public void ViewActivated(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
		{
			_ = ViewActivatedAsync(hitbloqLeaderboardViewController, firstActivation);
		}

		private async Task ViewActivatedAsync(HitbloqLeaderboardViewController hitbloqLeaderboardViewController, bool firstActivation)
		{
			if (firstActivation)
			{
				var hitbloqEvent = await _eventSource.GetAsync();
				if (hitbloqEvent != null && hitbloqEvent.ID != -1)
				{
					if (!PluginConfig.Instance.ViewedEvents.Contains(hitbloqEvent.ID))
					{
						await UnityMainThreadTaskScheduler.Factory.StartNew(() => ShowModal(hitbloqLeaderboardViewController.transform));
						PluginConfig.Instance.ViewedEvents.Add(hitbloqEvent.ID);
						PluginConfig.Instance.Changed();
					}
				}
			}
		}

		private void Parse(Transform parentTransform)
		{
			if (!_parsed)
			{
				BSMLParser.Instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.HitbloqEventModal.bsml"), parentTransform.gameObject, this);
				_modalPosition = _modalTransform!.localPosition;
			}

			_modalTransform!.SetParent(parentTransform);
			_modalTransform.localPosition = _modalPosition!.Value;
			Accessors.AnimateCanvasAccessor(ref _modalView!) = true;
			_descriptionTextPage!.ScrollTo(0, true);
		}

		internal void ShowModal(Transform parentTransform)
		{
			Parse(parentTransform);
			_parserParams?.EmitEvent("close-modal");
			_parserParams?.EmitEvent("open-modal");
		}

		[UIAction("#post-parse")]
		private void PostParse()
		{
			_ = PostParseAsync();
		}

		private async Task PostParseAsync()
		{
			_parsed = true;
			_modalView!.gameObject.name = "HitbloqEventModal";
			_currentEvent = await _eventSource.GetAsync();

			if (_currentEvent?.Image != null)
			{
				_ = _spriteLoader.DownloadSpriteAsync(_currentEvent.Image, sprite => _eventImage!.sprite = sprite);
			}

			NotifyPropertyChanged(nameof(EventTitle));
			NotifyPropertyChanged(nameof(EventDescription));
			NotifyPropertyChanged(nameof(PoolExists));
		}

		[UIAction("pool-click")]
		private void PoolClick()
		{
			if (PoolExists)
			{
				_hitbloqFlowCoordinator.ShowAndOpenPoolWithID(_currentEvent!.Pool);
			}
		}
	}
}