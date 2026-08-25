using System;
using System.Collections;
using IsekaiTruck.Camera;
using IsekaiTruck.Collection;
using IsekaiTruck.Input;
using IsekaiTruck.Monsters;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class MonsterCollectionUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private GameObject overlay;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private MonsterCollectionBookView bookView;
        [SerializeField] private MonsterCollectionCardView[] cards;

        private MonsterCollectionSystem collectionSystem;
        private JoystickInput joystickInput;
        private GameUIController gameUIController;
        private Func<bool> isOtherMenuOpen;
        private Coroutine transitionRoutine;
        private bool isInitialized;
        private bool isTransitioning;

        public bool IsPanelOpen => overlay != null && overlay.activeSelf;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (IsPanelOpen && !isTransitioning && keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                Close();
            }
        }

        public void Initialize(
            MonsterCollectionSystem system,
            JoystickInput input,
            CameraController cameraController,
            GameUIController gameUI,
            Func<bool> otherMenuOpenCheck)
        {
            collectionSystem = system;
            joystickInput = input;
            gameUIController = gameUI;
            isOtherMenuOpen = otherMenuOpenCheck;

            if (cards.Length != collectionSystem.Catalog.Entries.Count)
            {
                throw new MissingReferenceException("도감 카드 수와 카탈로그 항목 수가 일치하지 않습니다.");
            }

            openButton.onClick.AddListener(Open);
            closeButton.onClick.AddListener(Close);
            collectionSystem.MonsterUnlocked += HandleMonsterUnlocked;

            for (int i = 0; i < cards.Length; i++)
            {
                MonsterCollectionEntry entry = collectionSystem.Catalog.Entries[i];
                cards[i].Initialize(entry, collectionSystem.IsUnlocked(entry.TypeId), HandleCardClicked);
            }

            overlay.SetActive(false);
            SetViewport(cameraController.ViewportRect);
            isInitialized = true;
        }

        public void SetViewport(Rect viewport)
        {
            gameArea.anchorMin = viewport.min;
            gameArea.anchorMax = viewport.max;
            gameArea.offsetMin = Vector2.zero;
            gameArea.offsetMax = Vector2.zero;
        }

        private void Open()
        {
            if (!isInitialized || IsPanelOpen || isTransitioning || (isOtherMenuOpen != null && isOtherMenuOpen()))
            {
                return;
            }

            gameUIController.SetCollectionNotificationVisible(false);
            joystickInput.SetInputEnabled(false);
            overlay.SetActive(true);
            overlay.transform.SetAsLastSibling();
            transitionRoutine = StartCoroutine(OpenRoutine());
        }

        private void Close()
        {
            if (!IsPanelOpen || isTransitioning)
            {
                return;
            }

            transitionRoutine = bookView.HasSelection
                ? StartCoroutine(CloseSelectionRoutine())
                : StartCoroutine(CloseRoutine());
        }

        private IEnumerator OpenRoutine()
        {
            isTransitioning = true;
            yield return bookView.PlayOpen();
            isTransitioning = false;
            transitionRoutine = null;
        }

        private IEnumerator CloseRoutine()
        {
            isTransitioning = true;
            yield return bookView.PlayClose();
            overlay.SetActive(false);
            joystickInput.SetInputEnabled(true);
            isTransitioning = false;
            transitionRoutine = null;
        }

        private IEnumerator CloseSelectionRoutine()
        {
            isTransitioning = true;
            yield return bookView.PlayCloseSelection();
            isTransitioning = false;
            transitionRoutine = null;
        }

        private void HandleCardClicked(MonsterCollectionCardView card)
        {
            if (isTransitioning || !card.IsUnlocked)
            {
                return;
            }

            transitionRoutine = StartCoroutine(SelectCardRoutine(card));
        }

        private IEnumerator SelectCardRoutine(MonsterCollectionCardView card)
        {
            isTransitioning = true;
            MonsterCollectionEntry entry = card.Entry;
            MonsterData monster = entry.Definition.CreateData();
            string detail =
                $"<b>특징</b>\n{entry.BehaviorDescription}\n\n" +
                $"<b>전송 보상</b>\nEXP +{monster.Exp}    영혼 +{monster.Soul}\n\n" +
                $"<b>전송 팁</b>\n{entry.DefeatDescription}";
            yield return bookView.PlaySelection(card, entry, detail);
            isTransitioning = false;
            transitionRoutine = null;
        }

        private void HandleMonsterUnlocked(string typeId)
        {
            for (int i = 0; i < cards.Length; i++)
            {
                if (cards[i].Entry.TypeId == typeId)
                {
                    cards[i].SetUnlocked(true);
                    break;
                }
            }

            if (!IsPanelOpen)
            {
                gameUIController.SetCollectionNotificationVisible(true);
            }
        }

        private void OnDestroy()
        {
            if (transitionRoutine != null)
            {
                StopCoroutine(transitionRoutine);
            }

            if (openButton != null) openButton.onClick.RemoveListener(Open);
            if (closeButton != null) closeButton.onClick.RemoveListener(Close);
            if (collectionSystem != null) collectionSystem.MonsterUnlocked -= HandleMonsterUnlocked;
        }

#if UNITY_EDITOR
        public void SetOpenButton(Button targetOpenButton)
        {
            openButton = targetOpenButton;
        }

        public void SetReferences(
            RectTransform targetGameArea,
            GameObject targetOverlay,
            Button targetOpenButton,
            Button targetCloseButton,
            MonsterCollectionBookView targetBookView,
            MonsterCollectionCardView[] targetCards)
        {
            gameArea = targetGameArea;
            overlay = targetOverlay;
            openButton = targetOpenButton;
            closeButton = targetCloseButton;
            bookView = targetBookView;
            cards = targetCards;
        }
#endif
    }
}
