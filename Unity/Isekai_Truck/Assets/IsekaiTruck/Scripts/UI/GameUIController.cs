using IsekaiTruck.Camera;
using IsekaiTruck.Input;
using IsekaiTruck.Player;
using IsekaiTruck.Truck;
using IsekaiTruck.Upgrades;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class GameUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private Text levelText;
        [SerializeField] private Text expText;
        [SerializeField] private Image expFill;
        [SerializeField] private Text soulText;
        [SerializeField] private Text pointText;
        [SerializeField] private Text upgradePointText;
        [SerializeField] private Text speedLevelText;
        [SerializeField] private Text sizeLevelText;
        [SerializeField] private Text speedStatText;
        [SerializeField] private Text sizeStatText;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button sizeButton;

        private PlayerState playerState;
        private TruckController truckController;
        private TruckUpgradeSystem upgradeSystem;
        private JoystickInput joystickInput;

        public bool IsUpgradePanelOpen => upgradePanel != null && upgradePanel.activeSelf;

        public void Initialize(
            PlayerState state,
            TruckController truck,
            TruckUpgradeSystem upgrades,
            JoystickInput input,
            CameraController cameraController
        )
        {
            playerState = state;
            truckController = truck;
            upgradeSystem = upgrades;
            joystickInput = input;

            playerState.StateChanged += HandlePlayerStateChanged;
            upgradeSystem.UpgradeApplied += HandleUpgradeApplied;
            openButton.onClick.AddListener(OpenUpgradePanel);
            closeButton.onClick.AddListener(CloseUpgradePanel);
            speedButton.onClick.AddListener(UpgradeSpeed);
            sizeButton.onClick.AddListener(UpgradeSize);

            upgradePanel.SetActive(false);
            joystickInput.SetInputEnabled(true);
            SetViewport(cameraController.ViewportRect);
            Refresh();
        }

        public void SetViewport(Rect viewport)
        {
            gameArea.anchorMin = viewport.min;
            gameArea.anchorMax = viewport.max;
            gameArea.offsetMin = Vector2.zero;
            gameArea.offsetMax = Vector2.zero;
        }

        public void Refresh()
        {
            PlayerSnapshot player = playerState.GetState();
            TruckController.TruckStats truck = truckController.GetStats();
            float expRatio = player.RequiredExp > 0 ? (float)player.Exp / player.RequiredExp : 0f;

            levelText.text = $"Lv. {player.Level}";
            expText.text = $"EXP {player.Exp} / {player.RequiredExp}";
            RectTransform expFillRect = expFill.rectTransform;
            Vector2 expFillAnchorMax = expFillRect.anchorMax;
            expFillAnchorMax.x = Mathf.Clamp01(expRatio);
            expFillRect.anchorMax = expFillAnchorMax;
            soulText.text = $"영혼 {player.Soul}";
            pointText.text = $"포인트 {player.UpgradePoints}";
            upgradePointText.text = $"남은 포인트: {player.UpgradePoints}";
            speedLevelText.text = $"Lv.{truck.SpeedLevel}";
            sizeLevelText.text = $"Lv.{truck.SizeLevel}";
            speedStatText.text = $"최대 속도: {truck.MaxSpeed:F3}";
            sizeStatText.text = $"트럭 크기: {Mathf.RoundToInt(truck.SizeScale * 100f)}%";

            bool canUpgrade = player.UpgradePoints > 0;
            speedButton.interactable = canUpgrade;
            sizeButton.interactable = canUpgrade;
        }

        private void OpenUpgradePanel()
        {
            upgradePanel.SetActive(true);
            joystickInput.SetInputEnabled(false);
            Refresh();
        }

        private void CloseUpgradePanel()
        {
            upgradePanel.SetActive(false);
            joystickInput.SetInputEnabled(true);
        }

        private void UpgradeSpeed()
        {
            upgradeSystem.TryUpgradeSpeed();
            Refresh();
        }

        private void UpgradeSize()
        {
            upgradeSystem.TryUpgradeSize();
            Refresh();
        }

        private void HandlePlayerStateChanged(PlayerSnapshot state)
        {
            Refresh();
        }

        private void HandleUpgradeApplied(TruckUpgradeResult result)
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (playerState != null)
            {
                playerState.StateChanged -= HandlePlayerStateChanged;
            }

            if (upgradeSystem != null)
            {
                upgradeSystem.UpgradeApplied -= HandleUpgradeApplied;
            }

            if (openButton != null) openButton.onClick.RemoveListener(OpenUpgradePanel);
            if (closeButton != null) closeButton.onClick.RemoveListener(CloseUpgradePanel);
            if (speedButton != null) speedButton.onClick.RemoveListener(UpgradeSpeed);
            if (sizeButton != null) sizeButton.onClick.RemoveListener(UpgradeSize);
        }

#if UNITY_EDITOR
        public void SetReferences(
            RectTransform targetGameArea,
            GameObject targetUpgradePanel,
            Text targetLevelText,
            Text targetExpText,
            Image targetExpFill,
            Text targetSoulText,
            Text targetPointText,
            Text targetUpgradePointText,
            Text targetSpeedLevelText,
            Text targetSizeLevelText,
            Text targetSpeedStatText,
            Text targetSizeStatText,
            Button targetOpenButton,
            Button targetCloseButton,
            Button targetSpeedButton,
            Button targetSizeButton
        )
        {
            gameArea = targetGameArea;
            upgradePanel = targetUpgradePanel;
            levelText = targetLevelText;
            expText = targetExpText;
            expFill = targetExpFill;
            soulText = targetSoulText;
            pointText = targetPointText;
            upgradePointText = targetUpgradePointText;
            speedLevelText = targetSpeedLevelText;
            sizeLevelText = targetSizeLevelText;
            speedStatText = targetSpeedStatText;
            sizeStatText = targetSizeStatText;
            openButton = targetOpenButton;
            closeButton = targetCloseButton;
            speedButton = targetSpeedButton;
            sizeButton = targetSizeButton;
        }
#endif
    }
}
