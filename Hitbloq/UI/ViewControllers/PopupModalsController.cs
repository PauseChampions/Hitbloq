using System;
using System.Reflection;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using Hitbloq.Utilities;
using HMUI;
using UnityEngine;

namespace Hitbloq.UI
{
    internal class PopupModalsController : NotifiableBase
    {
        private readonly MainMenuViewController mainMenuViewController;
        private bool parsed;
        
        private Action? yesButtonPressed;
        private Action? noButtonPressed;
        
        private string yesNoText = "";
        private string yesButtonText = "Yes";
        private string noButtonText = "No";

        [UIComponent("yes-no-modal")]
        private readonly RectTransform yesNoModalTransform = null!;

        [UIComponent("yes-no-modal")]
        private ModalView yesNoModalView = null!;

        private Vector3 yesNoModalPosition;
        
        [UIParams]
        private readonly BSMLParserParams parserParams = null!;

        public PopupModalsController(MainMenuViewController mainMenuViewController)
        {
            this.mainMenuViewController = mainMenuViewController;
        }

        private void Parse()
        {
            if (!parsed)
            {
                BSMLParser.instance.Parse(BeatSaberMarkupLanguage.Utilities.GetResourceContent(Assembly.GetExecutingAssembly(), "Hitbloq.UI.Views.PopupModals.bsml"), mainMenuViewController.gameObject, this);
                yesNoModalPosition = yesNoModalTransform.localPosition;
                parsed = true;
            }
        }

        #region Yes/No Modal

        // Methods

        internal void ShowYesNoModal(Transform parent, string text, Action yesButtonPressedCallback, string yesButtonText = "Yes", string noButtonText = "No", Action? noButtonPressedCallback = null, bool animateParentCanvas = true)
        {
            Parse();
            yesNoModalTransform.localPosition = yesNoModalPosition;
            yesNoModalTransform.transform.SetParent(parent);

            YesNoText = text;
            YesButtonText = yesButtonText;
            NoButtonText = noButtonText;

            yesButtonPressed = yesButtonPressedCallback;
            noButtonPressed = noButtonPressedCallback;

            Accessors.AnimateCanvasAccessor(ref yesNoModalView) = animateParentCanvas;
            Accessors.ViewValidAccessor(ref yesNoModalView) = false; // Need to do this to show the animation after parent changes

            parserParams.EmitEvent("close-yes-no");
            parserParams.EmitEvent("open-yes-no");
        }

        internal void HideYesNoModal() => parserParams.EmitEvent("close-yes-no");

        [UIAction("yes-button-pressed")]
        private void YesButtonPressed()
        {
            yesButtonPressed?.Invoke();
            yesButtonPressed = null;
        }

        [UIAction("no-button-pressed")]
        private void NoButtonPressed()
        {
            noButtonPressed?.Invoke();
            noButtonPressed = null;
        }

        // Values

        [UIValue("yes-no-text")]
        private string YesNoText
        {
            get => yesNoText;
            set
            {
                yesNoText = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue("yes-button-text")]
        private string YesButtonText
        {
            get => yesButtonText;
            set
            {
                yesButtonText = value;
                NotifyPropertyChanged();
            }
        }

        [UIValue("no-button-text")]
        private string NoButtonText
        {
            get => noButtonText;
            set
            {
                noButtonText = value;
                NotifyPropertyChanged();
            }
        }
        
        #endregion
    }
}