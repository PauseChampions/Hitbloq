using System;
using System.Collections;
using System.Collections.Generic;
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
		private const string CellClickTargetName = "HitbloqCellClickTarget";
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

		public Vector3 WorldPosition => _cellClickerImage != null ? _cellClickerImage.transform.position : Vector3.zero;

		public static void ClearCellClickers(IEnumerable<LeaderboardTableCell> leaderboardTableCells)
		{
			foreach (var leaderboardTableCell in leaderboardTableCells)
			{
				foreach (var cellClicker in leaderboardTableCell.GetComponentsInChildren<CellClicker>(true))
				{
					// Edited by GPT-5 Codex 2026-05-27
					// Unity also delays component destruction, so SetCellClicker can otherwise
					// grab a CellClicker that is already scheduled to be destroyed.
					// Reset and reuse the component so hover/click does not disappear later.
					cellClicker.Clear();
				}

				var target = leaderboardTableCell.transform.Find(CellClickTargetName);
				if (target != null)
				{
					// Edited by GPT-5 Codex 2026-05-27
					// Unity destroys objects at the end of the frame, so destroying this click target
					// during a leaderboard rebuild can leave SetCellClicker reusing an object about to die.
					// Disable and reuse the target instead; SetCellClicker re-enables raycasts below.
					var image = target.GetComponent<Image>();
					if (image != null)
					{
						image.raycastTarget = false;
					}

					target.gameObject.SetActive(false);
				}
			}
		}

		public static void SetCellClicker(LeaderboardTableCell leaderboardTableCell, int index, Action<int> onClick, Image? separator)
		{
			leaderboardTableCell.interactable = true;
			if (leaderboardTableCell.GetComponent<Touchable>() == null)
			{
				leaderboardTableCell.gameObject.AddComponent<Touchable>();
			}

            var clickTarget = leaderboardTableCell.transform.Find(CellClickTargetName)?.gameObject;
			if (clickTarget == null)
			{
				// Don't add Touchable on the same GameObject as an Image/Graphic: Unity disallows multiple Graphic
				// components on the same GameObject. The parent leaderboardTableCell already receives a Touchable
				// above, so only add RectTransform and Image here.
				clickTarget = new GameObject(CellClickTargetName, typeof(RectTransform), typeof(Image));
				clickTarget.transform.SetParent(leaderboardTableCell.transform, false);
			}

			var rectTransform = clickTarget.GetComponent<RectTransform>();
			rectTransform.anchorMin = Vector2.zero;
			rectTransform.anchorMax = Vector2.one;
			rectTransform.offsetMin = Vector2.zero;
			rectTransform.offsetMax = Vector2.zero;
			rectTransform.localScale = Vector3.one;
			rectTransform.SetAsLastSibling();

			var image = clickTarget.GetComponent<Image>();
			image.sprite = BeatSaberMarkupLanguage.Utilities.ImageResources.BlankSprite;
			image.color = new Color(1f, 1f, 1f, 0.001f);
			image.raycastTarget = true;
			clickTarget.SetActive(true);

			var cellClicker = clickTarget.GetComponent<CellClicker>() ?? clickTarget.AddComponent<CellClicker>();
			cellClicker.Index = index;
			cellClicker.OnClick = onClick;
			cellClicker.Separator = separator;
		}

		private sealed class CellClicker : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
		{
			private static readonly Color HighlightColor = Color.white;
			private static CellClicker? _activeClicker;

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

				if (_activeClicker != null && _activeClicker != this)
				{
					_activeClicker.ResetSeparator();
				}

				_activeClicker = this;
				CacheOriginals();
				StopAllCoroutines();
				Separator.transform.localScale = _originalScale!.Value * 2f;
				StartCoroutine(LerpSeparator(Separator.color, HighlightColor, GetImageViewColor0(Separator), HighlightColor, GetImageViewColor1(Separator), new Color(1f, 1f, 1f, 0f), 0.15f));
			}

			public void OnPointerExit(PointerEventData eventData)
			{
				if (_activeClicker == this)
				{
					_activeClicker = null;
				}

				ResetSeparator(0.05f);
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

			private void ResetSeparator(float duration = 0f)
			{
				if (Separator == null || _originalColor == null || _originalColor0 == null || _originalColor1 == null || _originalScale == null)
				{
					return;
				}

				StopAllCoroutines();
				Separator.transform.localScale = _originalScale.Value;
				if (duration <= 0f)
				{
					Separator.color = _originalColor.Value;
					SetImageViewColors(Separator, _originalColor0.Value, _originalColor1.Value);
					return;
				}

				StartCoroutine(LerpSeparator(Separator.color, _originalColor.Value, GetImageViewColor0(Separator), _originalColor0.Value, GetImageViewColor1(Separator), _originalColor1.Value, duration));
			}

			public void Clear()
			{
				// Edited by GPT-5 Codex 2026-05-27
				// Clearing replaces delayed Destroy during leaderboard rebuilds.
				// It restores hover state and removes the callback until SetCellClicker assigns a new row.
				StopAllCoroutines();
				if (_activeClicker == this)
				{
					_activeClicker = null;
				}

				ResetSeparator();
				OnClick = null;
				Separator = null;
			}

			private void OnDestroy()
			{
				StopAllCoroutines();
				if (_activeClicker == this)
				{
					_activeClicker = null;
				}

				ResetSeparator();
				OnClick = null;
				Separator = null;
			}
		}
	}
}
