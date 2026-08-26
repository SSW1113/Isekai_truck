using System;
using IsekaiTruck.Blessings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class BlessingCardView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
    {
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private RectTransform iconRect;
        [SerializeField] private Image background;
        [SerializeField] private Image icon;
        [SerializeField] private Image spotlight;
        [SerializeField] private Outline border;
        [SerializeField] private Text gradeText;
        [SerializeField] private Text nameText;
        [SerializeField] private Text descriptionText;
        [SerializeField] private Text ownedText;
        [SerializeField, Min(1f)] private float transitionSpeed = 12f;
        [SerializeField, Range(1f, 1.1f)] private float hoverScale = 1.045f;

        private readonly Color normalBackground = new Color32(0xFF, 0xF4, 0xD8, 0xFF);
        private readonly Color hoverBackground = new Color32(0xF5, 0xC5, 0xDC, 0xFF);
        private readonly Color normalSpotlight = new Color(1f, 0.83f, 0.42f, 0f);
        private readonly Color hoverSpotlight = new Color(1f, 0.83f, 0.42f, 0.22f);

        private int candidateIndex = -1;
        private bool isHovered;
        private Color normalBorder;
        private Color hoverBorder;

        public event Action<int> Selected;

        public void SetReferences(
            RectTransform targetCardRect,
            RectTransform targetIconRect,
            Image targetBackground,
            Image targetIcon,
            Image targetSpotlight,
            Outline targetBorder,
            Text targetGradeText,
            Text targetNameText,
            Text targetDescriptionText,
            Text targetOwnedText
        )
        {
            cardRect = targetCardRect;
            iconRect = targetIconRect;
            background = targetBackground;
            icon = targetIcon;
            spotlight = targetSpotlight;
            border = targetBorder;
            gradeText = targetGradeText;
            nameText = targetNameText;
            descriptionText = targetDescriptionText;
            ownedText = targetOwnedText;
        }

        public void SetData(BlessingDefinition blessing, int index, int ownedCount)
        {
            candidateIndex = index;
            gradeText.text = blessing != null ? $"{blessing.Grade} 등급" : string.Empty;
            nameText.text = blessing != null ? blessing.DisplayName : string.Empty;
            descriptionText.text = blessing != null ? blessing.Description : string.Empty;
            ownedText.text = blessing != null ? $"보유 {ownedCount}개" : string.Empty;

            Color gradeColor = GetGradeColor(blessing != null ? blessing.Grade : BlessingGrade.C);
            icon.color = gradeColor;
            gradeText.color = gradeColor;
            normalBorder = new Color(gradeColor.r, gradeColor.g, gradeColor.b, 0.76f);
            hoverBorder = Color.Lerp(gradeColor, Color.white, 0.34f);
            ResetVisuals();
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            isHovered = true;
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            isHovered = false;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (candidateIndex >= 0 && eventData.button == PointerEventData.InputButton.Left)
            {
                Selected?.Invoke(candidateIndex);
            }
        }

        private void Update()
        {
            float blend = 1f - Mathf.Exp(-transitionSpeed * Time.unscaledDeltaTime);
            float targetScale = isHovered ? hoverScale : 1f;
            cardRect.localScale = Vector3.Lerp(cardRect.localScale, Vector3.one * targetScale, blend);
            iconRect.localScale = Vector3.Lerp(iconRect.localScale, Vector3.one * (isHovered ? 1.06f : 1f), blend);
            background.color = Color.Lerp(background.color, isHovered ? hoverBackground : normalBackground, blend);
            border.effectColor = Color.Lerp(border.effectColor, isHovered ? hoverBorder : normalBorder, blend);
            spotlight.color = Color.Lerp(spotlight.color, isHovered ? hoverSpotlight : normalSpotlight, blend);
        }

        private void OnDisable()
        {
            candidateIndex = -1;
            isHovered = false;
            ResetVisuals();
        }

        private void ResetVisuals()
        {
            if (cardRect != null) cardRect.localScale = Vector3.one;
            if (iconRect != null) iconRect.localScale = Vector3.one;
            if (background != null) background.color = normalBackground;
            if (border != null) border.effectColor = normalBorder;
            if (spotlight != null) spotlight.color = normalSpotlight;
        }

        private static Color GetGradeColor(BlessingGrade grade)
        {
            switch (grade)
            {
                case BlessingGrade.U:
                    return new Color(0.45f, 0.84f, 0.96f, 1f);
                case BlessingGrade.R:
                    return new Color(0.70f, 0.44f, 0.92f, 1f);
                case BlessingGrade.SR:
                    return new Color(0.94f, 0.62f, 0.84f, 1f);
                default:
                    return new Color(0.78f, 0.82f, 0.86f, 1f);
            }
        }
    }
}
