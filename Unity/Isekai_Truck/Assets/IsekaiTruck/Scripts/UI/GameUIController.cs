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
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button speedButton;
        [SerializeField] private Button sizeButton;
        [SerializeField] private Button collectionButton;
        [SerializeField] private GameObject collectionNotificationBadge;
        [SerializeField] private GameObject upgradeAvailableIndicator;
        [SerializeField] private UIFeedbackEffect levelFeedback;
        [SerializeField] private UIFeedbackEffect soulFeedback;
        [SerializeField] private UIFeedbackEffect upgradeFeedback;
        [SerializeField] private UIFeedbackEffect speedFeedback;
        [SerializeField] private SpeedHUDView speedHudView;

        private PlayerState playerState;
        private TruckController truckController;
        private TruckUpgradeSystem upgradeSystem;
        private JoystickInput joystickInput;
        private int renderedSpeedKmh = int.MinValue;
        private int renderedSoul = int.MinValue;
        private int lastSpeedFeedbackKmh;
        private int targetSoul;
        private float displayedSoul;
        private float soulVelocity;
        private float displayedSpeedKmh;
        private float targetSpeedKmh;
        private float currentExpRatio;
        private float targetExpRatio;
        private bool hasDisplayedState;
        private bool hasDisplayedSoul;
        private bool hasDisplayedSpeed;
        private bool isDeferringSoulReward;
        private int deferredSoul;
        private PlayerSnapshot playerStateSnapshot;

        private const float ExpBarFollowSpeed = 10f;
        private const float SoulSmoothTime = 0.22f;
        private const float SpeedFollowSpeed = 9f;
        private const int SpeedFeedbackStepKmh = 8;
        private const int BaseDisplayedMaxSpeedKmh = 40;
        private const int DisplayedSpeedPerUpgradeKmh = 10;

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
            isDeferringSoulReward = false;
            deferredSoul = 0;
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
            UpdateExpBar();
            UpdateSoulDisplay();
            UpdateSpeedDisplay();
        }

        public void Refresh()
        {
            PlayerSnapshot player = playerState.GetState();
            RefreshPlayer(player, false);

            TruckController.TruckStats truck = truckController.GetStats();
            upgradePointText.text = $"남은 포인트: {player.UpgradePoints}";
            speedLevelText.text = $"Lv.{truck.SpeedLevel}";
            sizeLevelText.text = $"Lv.{truck.SizeLevel}";
            int displayedMaxSpeedKmh = BaseDisplayedMaxSpeedKmh + truck.SpeedLevel * DisplayedSpeedPerUpgradeKmh;
            RefreshSpeedTarget();
        }

        private void RefreshPlayer(PlayerSnapshot player, bool animateChanges)
        {
            int previousLevel = hasDisplayedState ? playerStateSnapshot.Level : player.Level;
            int previousSoul = hasDisplayedState ? playerStateSnapshot.Soul : player.Soul;
            int previousPoints = hasDisplayedState ? playerStateSnapshot.UpgradePoints : player.UpgradePoints;
            float expRatio = player.RequiredExp > 0 ? (float)player.Exp / player.RequiredExp : 0f;

            levelText.text = $"Lv. {player.Level}";
            expText.text = $"{player.Exp} / {player.RequiredExp}";
            pointText.text = $"포인트 {player.UpgradePoints}";

            if (!isDeferringSoulReward)
            {
                targetSoul = Mathf.Max(0, player.Soul - deferredSoul);
            }
            if (!hasDisplayedSoul || !animateChanges)
            {
                displayedSoul = targetSoul;
                soulVelocity = 0f;
                ApplySoulDisplay();
                hasDisplayedSoul = true;
            }

            bool canUpgrade = player.UpgradePoints > 0;
            speedButton.interactable = canUpgrade;
            sizeButton.interactable = canUpgrade;
            if (upgradeAvailableIndicator != null)
            {
                upgradeAvailableIndicator.SetActive(canUpgrade);
            }

            targetExpRatio = Mathf.Clamp01(expRatio);
            if (!hasDisplayedState || !animateChanges)
            {
                currentExpRatio = targetExpRatio;
                ApplyExpRatio(currentExpRatio);
            }

            if (animateChanges && hasDisplayedState)
            {
                if (player.Level > previousLevel) levelFeedback?.Play();
                if (player.Soul > previousSoul && !isDeferringSoulReward) soulFeedback?.Play();
                if (previousPoints <= 0 && player.UpgradePoints > 0) upgradeFeedback?.Play();
            }

            playerStateSnapshot = player;
            hasDisplayedState = true;
        }

        public void BeginDeferredSoulReward()
        {
            isDeferringSoulReward = true;
        }

        public void QueueDeferredSoulReward(int soulAmount)
        {
            if (!isDeferringSoulReward)
            {
                return;
            }

            isDeferringSoulReward = false;
            deferredSoul += Mathf.Max(0, soulAmount);
            targetSoul = Mathf.Max(0, playerStateSnapshot.Soul - deferredSoul);
        }

        public void ReleaseDeferredSoul(int soulAmount)
        {
            int releasedSoul = Mathf.Min(Mathf.Max(0, soulAmount), deferredSoul);
            if (releasedSoul <= 0)
            {
                return;
            }

            deferredSoul -= releasedSoul;
            targetSoul = Mathf.Max(0, playerStateSnapshot.Soul - deferredSoul);
            soulFeedback?.Play();
        }

        private void UpdateExpBar()
        {
            if (Mathf.Approximately(currentExpRatio, targetExpRatio))
            {
                return;
            }

            float blend = 1f - Mathf.Exp(-ExpBarFollowSpeed * Time.unscaledDeltaTime);
            currentExpRatio = Mathf.Lerp(currentExpRatio, targetExpRatio, blend);
            if (Mathf.Abs(currentExpRatio - targetExpRatio) < 0.0005f)
            {
                currentExpRatio = targetExpRatio;
            }

            ApplyExpRatio(currentExpRatio);
        }

        private void ApplyExpRatio(float ratio)
        {
            expFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(ratio), 1f);
        }

        private void UpdateSoulDisplay()
        {
            bool hasReachedTarget = renderedSoul == targetSoul && Mathf.Abs(displayedSoul - targetSoul) < 0.01f;
            if (!hasDisplayedSoul || hasReachedTarget)
            {
                return;
            }

            displayedSoul = Mathf.SmoothDamp(
                displayedSoul,
                targetSoul,
                ref soulVelocity,
                SoulSmoothTime,
                Mathf.Infinity,
                Time.unscaledDeltaTime
            );
            if (Mathf.Abs(displayedSoul - targetSoul) < 0.01f)
            {
                displayedSoul = targetSoul;
                soulVelocity = 0f;
            }

            ApplySoulDisplay();
        }

        private void ApplySoulDisplay()
        {
            int value = Mathf.Max(0, Mathf.RoundToInt(displayedSoul));
            if (value == renderedSoul)
            {
                return;
            }

            renderedSoul = value;
            soulText.text = value.ToString();
        }

        private void RefreshSpeedTarget()
        {
            if (truckController == null || speedText == null)
            {
                return;
            }

            targetSpeedKmh = Mathf.Max(0f, truckController.CurrentSpeedPerSecond * 3.6f);
            if (!hasDisplayedSpeed)
            {
                displayedSpeedKmh = targetSpeedKmh;
                hasDisplayedSpeed = true;
                lastSpeedFeedbackKmh = Mathf.RoundToInt(targetSpeedKmh);
                ApplySpeedDisplay();
            }
        }

        private void UpdateSpeedDisplay()
        {
            RefreshSpeedTarget();
            if (!hasDisplayedSpeed)
            {
                return;
            }

            float blend = 1f - Mathf.Exp(-SpeedFollowSpeed * Time.unscaledDeltaTime);
            displayedSpeedKmh = Mathf.Lerp(displayedSpeedKmh, targetSpeedKmh, blend);
            if (Mathf.Abs(displayedSpeedKmh - targetSpeedKmh) < 0.02f)
            {
                displayedSpeedKmh = targetSpeedKmh;
            }

            ApplySpeedDisplay();

            int actualSpeedKmh = Mathf.RoundToInt(targetSpeedKmh);
            if (actualSpeedKmh >= lastSpeedFeedbackKmh + SpeedFeedbackStepKmh)
            {
                lastSpeedFeedbackKmh = actualSpeedKmh;
                speedFeedback?.Play();
            }
            else if (actualSpeedKmh <= lastSpeedFeedbackKmh - SpeedFeedbackStepKmh)
            {
                lastSpeedFeedbackKmh = actualSpeedKmh;
            }
        }

        private void ApplySpeedDisplay()
        {
            int speedKmh = Mathf.Max(0, Mathf.RoundToInt(displayedSpeedKmh));
            float maximumSpeedKmh = Mathf.Max(0.01f, truckController.CurrentMaxSpeedPerSecond * 3.6f);
            speedHudView?.SetSpeed(displayedSpeedKmh, displayedSpeedKmh / maximumSpeedKmh);
            if (speedKmh == renderedSpeedKmh)
            {
                return;
            }

            renderedSpeedKmh = speedKmh;
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
            RefreshPlayer(state, true);
        }

        private void HandleUpgradeApplied(TruckUpgradeResult result)
        {
            Refresh();
        }

        public void SetCollectionNotificationVisible(bool isVisible)
        {
            if (collectionNotificationBadge != null)
            {
                collectionNotificationBadge.SetActive(isVisible);
            }
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
            Button targetOpenButton,
            Button targetCloseButton,
            Button targetSpeedButton,
            Button targetSizeButton,
            Button targetCollectionButton,
            GameObject targetCollectionNotificationBadge,
            GameObject targetUpgradeAvailableIndicator,
            UIFeedbackEffect targetLevelFeedback,
            UIFeedbackEffect targetSoulFeedback,
            UIFeedbackEffect targetUpgradeFeedback,
            UIFeedbackEffect targetSpeedFeedback,
            SpeedHUDView targetSpeedHudView
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
            openButton = targetOpenButton;
            closeButton = targetCloseButton;
            speedButton = targetSpeedButton;
            sizeButton = targetSizeButton;
            collectionButton = targetCollectionButton;
            collectionNotificationBadge = targetCollectionNotificationBadge;
            upgradeAvailableIndicator = targetUpgradeAvailableIndicator;
            levelFeedback = targetLevelFeedback;
            soulFeedback = targetSoulFeedback;
            upgradeFeedback = targetUpgradeFeedback;
            speedFeedback = targetSpeedFeedback;
            speedHudView = targetSpeedHudView;
        }
#endif
    }
}
