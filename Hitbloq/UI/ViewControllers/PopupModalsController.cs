using System;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Utilities;
using HMUI;
using UnityEngine;

namespace Hitbloq.UI.ViewControllers
{
	internal class PopupModalsController : NotifiableBase
	{
		private readonly MainMenuViewController _mainMenuViewController;

		[UIParams]
		private readonly BSMLParserParams _parserParams = null!;

		[UIComponent("yes-no-modal")]
		private readonly RectTransform _yesNoModalTransform = null!;

		private Action? _noButtonPressed;
		private string _noButtonText = "No";
		private bool _parsed;

		private Action? _yesButtonPressed;
		private string _yesButtonText = "Yes";

		private Vector3 _yesNoModalPosition;

		[UIComponent("yes-no-modal")]
		private ModalView _yesNoModalView = null!;

		private string _yesNoText = "";

		public PopupModalsController(MainMenuViewController mainMenuViewController)
		{
			_mainMenuViewController = mainMenuViewController;
		}

		private void Parse()
		{
			if (!_parsed)
			{
				BSMLParser.Instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.PopupModals.bsml"), _mainMenuViewController.gameObject, this);
				_yesNoModalPosition = _yesNoModalTransform.localPosition;
				_parsed = true;
			}
		}

		#region Yes/No Modal

		// Methods

		internal void ShowYesNoModal(Transform parent, string text, Action yesButtonPressedCallback, string yesButtonText = "Yes", string noButtonText = "No", Action? noButtonPressedCallback = null, bool animateParentCanvas = true)
		{
			Parse();
			_yesNoModalTransform.localPosition = _yesNoModalPosition;
			_yesNoModalTransform.transform.SetParent(parent);

			YesNoText = text;
			YesButtonText = yesButtonText;
			NoButtonText = noButtonText;

			_yesButtonPressed = yesButtonPressedCallback;
			_noButtonPressed = noButtonPressedCallback;

			Accessors.AnimateCanvasAccessor(ref _yesNoModalView) = animateParentCanvas;
			Accessors.ViewValidAccessor(ref _yesNoModalView) = false; // Need to do this to show the animation after parent changes

			_parserParams.EmitEvent("close-yes-no");
			_parserParams.EmitEvent("open-yes-no");
		}

		internal void HideYesNoModal()
		{
			_parserParams.EmitEvent("close-yes-no");
		}

		[UIAction("yes-button-pressed")]
		private void YesButtonPressed()
		{
			_yesButtonPressed?.Invoke();
			_yesButtonPressed = null;
		}

		[UIAction("no-button-pressed")]
		private void NoButtonPressed()
		{
			_noButtonPressed?.Invoke();
			_noButtonPressed = null;
		}

		// Values

		[UIValue("yes-no-text")]
		private string YesNoText
		{
			get => _yesNoText;
			set
			{
				_yesNoText = value;
				NotifyPropertyChanged();
			}
		}

		[UIValue("yes-button-text")]
		private string YesButtonText
		{
			get => _yesButtonText;
			set
			{
				_yesButtonText = value;
				NotifyPropertyChanged();
			}
		}

		[UIValue("no-button-text")]
		private string NoButtonText
		{
			get => _noButtonText;
			set
			{
				_noButtonText = value;
				NotifyPropertyChanged();
			}
		}

		#endregion
	}
}