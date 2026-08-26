using System;
using IsekaiTruck.Collection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class MonsterCollectionCardView : MonoBehaviour
    {
        [SerializeField] private RectTransform cardRect;
        [SerializeField] private Button button;
        [SerializeField] private Image portraitImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text questionText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private GameObject selectionFrame;

        private MonsterCollectionEntry entry;
        private Action<MonsterCollectionCardView> clicked;
        private bool isUnlocked;

        public RectTransform CardRect => cardRect;
        public MonsterCollectionEntry Entry => entry;
        public bool IsUnlocked => isUnlocked;

        public void Initialize(
            MonsterCollectionEntry collectionEntry,
            bool unlocked,
            Action<MonsterCollectionCardView> clickHandler)
        {
            entry = collectionEntry;
            clicked = clickHandler;
            button.onClick.RemoveListener(HandleClicked);
            button.onClick.AddListener(HandleClicked);
            SetUnlocked(unlocked);
            SetSelected(false);
            SetFocusHidden(false);
        }

        public void SetUnlocked(bool unlocked)
        {
            isUnlocked = unlocked;
            portraitImage.sprite = unlocked ? entry.Portrait : null;
            portraitImage.enabled = unlocked && entry.Portrait != null;
            nameText.text = unlocked ? entry.DisplayName : "???";
            questionText.gameObject.SetActive(!unlocked);
        }

        public void SetSelected(bool selected)
        {
            selectionFrame.SetActive(selected);
        }

        public void SetFocusHidden(bool isHidden)
        {
            canvasGroup.alpha = isHidden ? 0f : 1f;
            canvasGroup.blocksRaycasts = !isHidden;
        }

        private void HandleClicked()
        {
            if (isUnlocked)
            {
                clicked?.Invoke(this);
            }
        }

        private void OnDestroy()
        {
            if (button != null)
            {
                button.onClick.RemoveListener(HandleClicked);
            }
        }

#if UNITY_EDITOR
        public void SetReferences(
            RectTransform targetCardRect,
            Button targetButton,
            Image targetPortrait,
            TMP_Text targetNameText,
            TMP_Text targetQuestionText,
            CanvasGroup targetCanvasGroup,
            GameObject targetSelectionFrame)
        {
            cardRect = targetCardRect;
            button = targetButton;
            portraitImage = targetPortrait;
            nameText = targetNameText;
            questionText = targetQuestionText;
            canvasGroup = targetCanvasGroup;
            selectionFrame = targetSelectionFrame;
        }
#endif
    }
}
