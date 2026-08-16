using IsekaiTruck.Camera;
using IsekaiTruck.Input;
using IsekaiTruck.World;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class WorldTravelUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private Text currentWorldText;
        [SerializeField] private Button openButton;
        [SerializeField] private Text openButtonText;
        [SerializeField] private GameObject confirmationPopup;
        [SerializeField] private Text confirmationText;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button cancelButton;

        private WorldTravelSystem worldTravelSystem;
        private JoystickInput joystickInput;
        private int originalSiblingIndex;

        public bool IsPanelOpen => confirmationPopup != null && confirmationPopup.activeSelf;

        public void Initialize(WorldTravelSystem travelSystem, JoystickInput input, CameraController cameraController)
        {
            worldTravelSystem = travelSystem;
            joystickInput = input;
            originalSiblingIndex = transform.GetSiblingIndex();

            worldTravelSystem.StateChanged += Refresh;
            openButton.onClick.AddListener(OpenConfirmation);
            confirmButton.onClick.AddListener(ConfirmTravel);
            cancelButton.onClick.AddListener(CloseConfirmation);

            confirmationPopup.SetActive(false);
            SetViewport(cameraController.ViewportRect);
            Refresh(worldTravelSystem.GetState());
        }

        public void SetViewport(Rect viewport)
        {
            gameArea.anchorMin = viewport.min;
            gameArea.anchorMax = viewport.max;
            gameArea.offsetMin = Vector2.zero;
            gameArea.offsetMax = Vector2.zero;
        }

        private void Refresh(WorldTravelSnapshot state)
        {
            currentWorldText.text = state.CurrentWorld != null
                ? $"현재 세계: {state.CurrentWorld.DisplayName}"
                : "현재 세계: 없음";

            openButton.interactable = state.CanTravel;
            if (!state.HasOtherWorld)
            {
                openButtonText.text = "이동할 세계 없음";
            }
            else if (!state.CanTravel)
            {
                openButtonText.text = $"세계 이동 (지명수배 Lv.{state.RequiredWantedLevel})";
            }
            else
            {
                openButtonText.text = "세계 이동";
            }
        }

        private void OpenConfirmation()
        {
            if (!worldTravelSystem.CanTravel)
            {
                return;
            }

            confirmationText.text = "무작위로 선택된 다른 세계로 이동합니다.\n킬 수와 지명수배 레벨이 초기화됩니다.";
            confirmationPopup.SetActive(true);
            transform.SetAsLastSibling();
            joystickInput.SetInputEnabled(false);
        }

        private void ConfirmTravel()
        {
            if (worldTravelSystem.TryTravel(out WorldTravelResult result))
            {
                Debug.Log($"세계 이동 완료: {result.PreviousWorld.DisplayName} → {result.DestinationWorld.DisplayName}", this);
            }

            CloseConfirmation();
        }

        private void CloseConfirmation()
        {
            confirmationPopup.SetActive(false);
            RestoreSiblingOrder();
            joystickInput.SetInputEnabled(true);
        }

        private void RestoreSiblingOrder()
        {
            if (transform.parent != null)
            {
                transform.SetSiblingIndex(Mathf.Min(originalSiblingIndex, transform.parent.childCount - 1));
            }
        }

        private void OnDestroy()
        {
            if (worldTravelSystem != null) worldTravelSystem.StateChanged -= Refresh;
            if (openButton != null) openButton.onClick.RemoveListener(OpenConfirmation);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmTravel);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(CloseConfirmation);
        }

#if UNITY_EDITOR
        public void SetReferences(
            RectTransform targetGameArea,
            Text targetCurrentWorldText,
            Button targetOpenButton,
            Text targetOpenButtonText,
            GameObject targetConfirmationPopup,
            Text targetConfirmationText,
            Button targetConfirmButton,
            Button targetCancelButton
        )
        {
            gameArea = targetGameArea;
            currentWorldText = targetCurrentWorldText;
            openButton = targetOpenButton;
            openButtonText = targetOpenButtonText;
            confirmationPopup = targetConfirmationPopup;
            confirmationText = targetConfirmationText;
            confirmButton = targetConfirmButton;
            cancelButton = targetCancelButton;
        }
#endif
    }
}
