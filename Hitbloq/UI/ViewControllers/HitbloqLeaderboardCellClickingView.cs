using System;
using System.Collections;
using BeatSaberMarkupLanguage;
using BeatSaberMarkupLanguage.Attributes;
using HMUI;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Hitbloq.UI.ViewControllers
{
	internal sealed class HitbloqLeaderboardCellClickingView
	{
		private readonly Sprite _blankSprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
		private CellClicker? _cellClicker;

		[UIComponent("cell-clicker-image")]
		private readonly ImageView _cellClickerImage = null!;

		[UIAction("#post-parse")]
		private void PostParse()
		{
			_cellClickerImage.sprite = _blankSprite;
			_cellClickerImage.material = BeatSaberMarkupLanguage.Utilities.ImageResources.NoGlowMat;
			_cellClickerImage.color = new Color(1f, 1f, 1f, 0.001f);
			_cellClickerImage.gameObject.SetActive(false);
		}

		public void ClearClicker()
		{
			if (_cellClicker != null)
			{
				UnityEngine.Object.Destroy(_cellClicker);
				_cellClicker = null;
			}

			if (_cellClickerImage != null)
			{
				_cellClickerImage.gameObject.SetActive(false);
			}
		}

		public void SetClicker(int index, Action<int> onClick, Image? separator)
		{
			_cellClickerImage.gameObject.SetActive(true);
			_cellClicker = _cellClickerImage.gameObject.GetComponent<CellClicker>() ?? _cellClickerImage.gameObject.AddComponent<CellClicker>();
			_cellClicker.Index = index;
			_cellClicker.OnClick = onClick;
			_cellClicker.Separator = separator;
		}

		private sealed class CellClicker : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
		{
			private static readonly Color HighlightColor = Color.white;

			private Color? _originalColor;
			private Color? _originalColor0;
			private Color? _originalColor1;
			private Vector3? _originalScale;

			public int Index { get; set; }
			public Action<int>? OnClick { get; set; }
			public Image? Separator { get; set; }

			public void OnPointerClick(PointerEventData eventData)
			{
				BeatSaberUI.BasicUIAudioManager.HandleButtonClickEvent();
				OnClick?.Invoke(Index);
			}

			public void OnPointerEnter(PointerEventData eventData)
			{
				if (Separator == null)
				{
					return;
				}

				CacheOriginals();
				StopAllCoroutines();
				Separator.transform.localScale = _originalScale!.Value * 2f;
				StartCoroutine(LerpSeparator(Separator.color, HighlightColor, GetImageViewColor0(Separator), HighlightColor, GetImageViewColor1(Separator), new Color(1f, 1f, 1f, 0f), 0.15f));
			}

			public void OnPointerExit(PointerEventData eventData)
			{
				if (Separator == null || _originalColor == null || _originalColor0 == null || _originalColor1 == null || _originalScale == null)
				{
					return;
				}

				StopAllCoroutines();
				Separator.transform.localScale = _originalScale.Value;
				StartCoroutine(LerpSeparator(Separator.color, _originalColor.Value, GetImageViewColor0(Separator), _originalColor0.Value, GetImageViewColor1(Separator), _originalColor1.Value, 0.05f));
			}

			private void CacheOriginals()
			{
				if (Separator == null || _originalColor != null)
				{
					return;
				}

				_originalColor = Separator.color;
				_originalColor0 = GetImageViewColor0(Separator);
				_originalColor1 = GetImageViewColor1(Separator);
				_originalScale = Separator.transform.localScale;
			}

			private IEnumerator LerpSeparator(Color startColor, Color endColor, Color startColor0, Color endColor0, Color startColor1, Color endColor1, float duration)
			{
				if (Separator == null)
				{
					yield break;
				}

				var elapsedTime = 0f;
				while (elapsedTime < duration)
				{
					var t = elapsedTime / duration;
					Separator.color = Color.Lerp(startColor, endColor, t);
					SetImageViewColors(Separator, Color.Lerp(startColor0, endColor0, t), Color.Lerp(startColor1, endColor1, t));
					elapsedTime += Time.deltaTime;
					yield return null;
				}

				Separator.color = endColor;
				SetImageViewColors(Separator, endColor0, endColor1);
			}

			private static Color GetImageViewColor0(Image image)
			{
				return image is ImageView imageView ? imageView.color0 : image.color;
			}

			private static Color GetImageViewColor1(Image image)
			{
				return image is ImageView imageView ? imageView.color1 : new Color(image.color.r, image.color.g, image.color.b, 0f);
			}

			private static void SetImageViewColors(Image image, Color color0, Color color1)
			{
				if (image is not ImageView imageView)
				{
					return;
				}

				imageView.color0 = color0;
				imageView.color1 = color1;
			}

			private void OnDestroy()
			{
				StopAllCoroutines();
				if (Separator != null && _originalColor != null && _originalColor0 != null && _originalColor1 != null && _originalScale != null)
				{
					Separator.color = _originalColor.Value;
					SetImageViewColors(Separator, _originalColor0.Value, _originalColor1.Value);
					Separator.transform.localScale = _originalScale.Value;
				}

				OnClick = null;
				Separator = null;
			}
		}
	}
}
