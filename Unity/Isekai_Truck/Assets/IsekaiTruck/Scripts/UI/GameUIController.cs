using IsekaiTruck.Camera;
using IsekaiTruck.Input;
using IsekaiTruck.Player;
using IsekaiTruck.Truck;
using IsekaiTruck.Upgrades;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class GameUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform leftPanel;
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private RectTransform rightPanel;
        [SerializeField] private GameObject upgradePanel;
        [SerializeField] private TMP_Text levelText;
        [SerializeField] private TMP_Text expText;
        [SerializeField] private Image expFill;
        [SerializeField] private TMP_Text soulText;
        [SerializeField] private TMP_Text speedText;
        [SerializeField] private TMP_Text pointText;
        [SerializeField] private TMP_Text upgradePointText;
        [SerializeField] private TMP_Text speedLevelText;
        [SerializeField] private TMP_Text sizeLevelText;
        [SerializeField] private TMP_Text speedStatText;
        [SerializeField] private TMP_Text sizeStatText;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button sizeButton;

        private PlayerState playerState;
        private TruckController truckController;
        private TruckUpgradeSystem upgradeSystem;
        private JoystickInput joystickInput;
        private int displayedSpeedKmh = int.MinValue;

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

            leftPanel.anchorMin = Vector2.zero;
            leftPanel.anchorMax = new Vector2(viewport.xMin, 1f);
            leftPanel.offsetMin = Vector2.zero;
            leftPanel.offsetMax = Vector2.zero;
            leftPanel.gameObject.SetActive(viewport.xMin > 0.001f);

            rightPanel.anchorMin = new Vector2(viewport.xMax, 0f);
            rightPanel.anchorMax = Vector2.one;
            rightPanel.offsetMin = Vector2.zero;
            rightPanel.offsetMax = Vector2.zero;
            rightPanel.gameObject.SetActive(viewport.xMax < 0.999f);
        }

        private void Update()
        {
            RefreshSpeed();
        }

        public void Refresh()
        {
            PlayerSnapshot player = playerState.GetState();
            TruckController.TruckStats truck = truckController.GetStats();
            float expRatio = player.RequiredExp > 0 ? (float)player.Exp / player.RequiredExp : 0f;

            levelText.text = $"Lv. {player.Level}";
            expText.text = $"EXP {player.Exp} / {player.RequiredExp}";
            expFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(expRatio), 1f);
            soulText.text = player.Soul.ToString();
            pointText.text = $"포인트 {player.UpgradePoints}";
            upgradePointText.text = $"남은 포인트: {player.UpgradePoints}";
            speedLevelText.text = $"Lv.{truck.SpeedLevel}";
            sizeLevelText.text = $"Lv.{truck.SizeLevel}";
            speedStatText.text = $"최대 속도: {truck.MaxSpeed:F3}";
            sizeStatText.text = $"트럭 크기: {Mathf.RoundToInt(truck.SizeScale * 100f)}%";

            bool canUpgrade = player.UpgradePoints > 0;
            speedButton.interactable = canUpgrade;
            sizeButton.interactable = canUpgrade;
            RefreshSpeed();
        }

        private void RefreshSpeed()
        {
            if (truckController == null || speedText == null)
            {
                return;
            }

            int speedKmh = Mathf.Max(0, Mathf.RoundToInt(truckController.CurrentSpeedPerSecond * 3.6f));
            if (speedKmh == displayedSpeedKmh)
            {
                return;
            }

            displayedSpeedKmh = speedKmh;
            speedText.text = $"{speedKmh} km/h";
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
            RectTransform targetLeftPanel,
            RectTransform targetGameArea,
            RectTransform targetRightPanel,
            GameObject targetUpgradePanel,
            TMP_Text targetLevelText,
            TMP_Text targetExpText,
            Image targetExpFill,
            TMP_Text targetSoulText,
            TMP_Text targetSpeedText,
            TMP_Text targetPointText,
            TMP_Text targetUpgradePointText,
            TMP_Text targetSpeedLevelText,
            TMP_Text targetSizeLevelText,
            TMP_Text targetSpeedStatText,
            TMP_Text targetSizeStatText,
            Button targetOpenButton,
            Button targetCloseButton,
            Button targetSpeedButton,
            Button targetSizeButton
        )
        {
            leftPanel = targetLeftPanel;
            gameArea = targetGameArea;
            rightPanel = targetRightPanel;
            upgradePanel = targetUpgradePanel;
            levelText = targetLevelText;
            expText = targetExpText;
            expFill = targetExpFill;
            soulText = targetSoulText;
            speedText = targetSpeedText;
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
