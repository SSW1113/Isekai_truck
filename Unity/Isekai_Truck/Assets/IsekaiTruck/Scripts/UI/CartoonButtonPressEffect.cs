using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [RequireComponent(typeof(Button))]
    public sealed class CartoonButtonPressEffect : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        [SerializeField] private RectTransform pressTarget;
        [SerializeField] private float pressedOffset = 3f;

        private Button button;
        private Vector2 restingPosition;

        private void Awake()
        {
            button = GetComponent<Button>();
            if (pressTarget != null)
            {
                restingPosition = pressTarget.anchoredPosition;
            }
        }

        private void OnDisable()
        {
            SetPressed(false);
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (button != null && button.interactable)
            {
                SetPressed(true);
            }
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            SetPressed(false);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetPressed(false);
        }

#if UNITY_EDITOR
        public void SetTarget(RectTransform target)
        {
            pressTarget = target;
            restingPosition = target != null ? target.anchoredPosition : Vector2.zero;
        }
#endif

        private void SetPressed(bool isPressed)
        {
            if (pressTarget == null)
            {
                return;
            }

            pressTarget.anchoredPosition = restingPosition + (isPressed ? Vector2.down * pressedOffset : Vector2.zero);
        }
    }
}
