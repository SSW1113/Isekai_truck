using UnityEngine;
using UnityEngine.EventSystems;

namespace IsekaiTruck.Input
{
    [DisallowMultipleComponent]
    public sealed class JoystickInput : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] private RectTransform joystickBase;
        [SerializeField] private RectTransform stick;
        [SerializeField, Min(1f)] private float maxDistance = 40f;

        private Vector2 startPosition;
        private Vector2 move;
        private Rect currentViewport = new Rect(0f, 0f, 1f, 1f);
        private int activePointerId = int.MinValue;

        public Vector2 Move => move;

        public void SetViewport(Rect viewport)
        {
            if (viewport == currentViewport)
            {
                return;
            }

            currentViewport = viewport;

            RectTransform inputArea = (RectTransform)transform;
            inputArea.anchorMin = viewport.min;
            inputArea.anchorMax = viewport.max;
            inputArea.offsetMin = Vector2.zero;
            inputArea.offsetMax = Vector2.zero;
        }

        private void Awake()
        {
            ResetJoystick();
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (activePointerId != int.MinValue)
            {
                return;
            }

            activePointerId = eventData.pointerId;
            startPosition = eventData.position;
            joystickBase.position = eventData.position;
            joystickBase.gameObject.SetActive(true);
            stick.anchoredPosition = Vector2.zero;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            Vector2 delta = eventData.position - startPosition;
            Vector2 clampedDelta = Vector2.ClampMagnitude(delta, maxDistance);

            stick.anchoredPosition = clampedDelta;
            move.x = clampedDelta.x / maxDistance;
            move.y = -clampedDelta.y / maxDistance;
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (eventData.pointerId != activePointerId)
            {
                return;
            }

            ResetJoystick();
        }

        private void OnDisable()
        {
            ResetJoystick();
        }

        private void ResetJoystick()
        {
            activePointerId = int.MinValue;
            move = Vector2.zero;

            if (stick != null)
            {
                stick.anchoredPosition = Vector2.zero;
            }

            if (joystickBase != null)
            {
                joystickBase.gameObject.SetActive(false);
            }
        }
    }
}
