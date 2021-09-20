using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.Parser;
using HMUI;
using IPA.Utilities;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Hitbloq.UI
{
    internal class HitbloqProfileModalController
    {
        private bool parsed;

        [UIComponent("modal")]
        private ModalView modalView;

        [UIComponent("modal")]
        private readonly RectTransform modalTransform;

        private Vector3 modalPosition;

        [UIComponent("modal-profile-pic")]
        private ImageView modalProfilePic;

        [UIComponent("modal-info-vertical")]
        private Backgroundable modalInfoVertical;

        [UIParams]
        private readonly BSMLParserParams parserParams;

        public void Initialize()
        {
            parsed = false;
        }

        [UIAction("#post-parse")]
        private void PostParse()
        {
            parsed = true;

            Material noGlowRoundEdge = Resources.FindObjectsOfTypeAll<Material>().First(m => m.name == "UINoGlowRoundEdge");
            modalProfilePic.material = noGlowRoundEdge;

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
            modalView.SetField("_animateParentCanvas", true);
        }

        internal void ShowModal(Transform parentTransform)
        {
            Parse(parentTransform);
            parserParams.EmitEvent("close-modal");
            parserParams.EmitEvent("open-modal");
        }
    }
}
