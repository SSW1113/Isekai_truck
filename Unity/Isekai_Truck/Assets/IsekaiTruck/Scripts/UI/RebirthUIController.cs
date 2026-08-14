using IsekaiTruck.Blessings;
using IsekaiTruck.Camera;
using IsekaiTruck.Input;
using IsekaiTruck.Player;
using IsekaiTruck.Rebirth;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class RebirthUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private GameObject rebirthPanel;
        [SerializeField] private GameObject tierPanel;
        [SerializeField] private GameObject candidatePanel;
        [SerializeField] private Text statusText;
        [SerializeField] private Text guideText;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button confirmButton;
        [SerializeField] private Button[] tierButtons;
        [SerializeField] private Text[] tierLabels;
        [SerializeField] private Button[] candidateButtons;
        [SerializeField] private Text[] candidateLabels;

        private RebirthSystem rebirthSystem;
        private BlessingSystem blessingSystem;
        private PlayerState playerState;
        private JoystickInput joystickInput;
        private int selectedTierIndex = -1;

        public bool IsPanelOpen => rebirthPanel != null && rebirthPanel.activeSelf;

        public void Initialize(RebirthSystem rebirth, BlessingSystem blessings, PlayerState player, JoystickInput input, CameraController cameraController)
        {
            rebirthSystem = rebirth;
            blessingSystem = blessings;
            playerState = player;
            joystickInput = input;

            playerState.StateChanged += HandlePlayerStateChanged;
            rebirthSystem.StateChanged += Refresh;
            blessingSystem.StateChanged += Refresh;
            openButton.onClick.AddListener(OpenPanel);
            closeButton.onClick.AddListener(ClosePanel);
            confirmButton.onClick.AddListener(ConfirmRebirth);

            for (int i = 0; i < tierButtons.Length; i++)
            {
                int tierIndex = i;
                tierButtons[i].onClick.AddListener(() => SelectTier(tierIndex));
            }

            for (int i = 0; i < candidateButtons.Length; i++)
            {
                int candidateIndex = i;
                candidateButtons[i].onClick.AddListener(() => ChooseBlessing(candidateIndex));
            }

            rebirthPanel.SetActive(rebirthSystem.HasPendingRebirth);
            if (rebirthSystem.HasPendingRebirth)
            {
                joystickInput.SetInputEnabled(false);
            }

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
            if (rebirthSystem == null)
            {
                return;
            }

            int maxLevel = rebirthSystem.Tiers[rebirthSystem.MaxUnlockedTierIndex].RequiredLevel;
            statusText.text = $"획득 배율 x{rebirthSystem.RewardMultiplier:F1}  |  최대 환생 Lv.{maxLevel}  |  축복 {blessingSystem.TotalOwnedCount}개";

            bool hasPending = rebirthSystem.HasPendingRebirth;
            tierPanel.SetActive(!hasPending);
            candidatePanel.SetActive(hasPending);
            closeButton.interactable = !hasPending;

            if (hasPending)
            {
                RefreshCandidates();
                guideText.text = "여신의 축복을 하나 선택하세요. 후보는 다시 뽑을 수 없습니다.";
                return;
            }

            RefreshTiers();
        }

        private void RefreshTiers()
        {
            if (selectedTierIndex < 0 || selectedTierIndex > rebirthSystem.MaxUnlockedTierIndex)
            {
                selectedTierIndex = FindHighestEligibleTier();
            }

            for (int i = 0; i < tierButtons.Length; i++)
            {
                bool exists = i < rebirthSystem.Tiers.Length;
                tierButtons[i].gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                int level = rebirthSystem.Tiers[i].RequiredLevel;
                bool isUnlocked = i <= rebirthSystem.MaxUnlockedTierIndex;
                bool isEligible = isUnlocked && playerState.Level >= level;
                tierButtons[i].interactable = isEligible;
                string state = !isUnlocked ? "잠김" : isEligible ? (i == selectedTierIndex ? "선택됨" : "선택") : $"Lv.{level} 필요";
                tierLabels[i].text = $"Lv.{level} 환생\n{state}";
            }

            confirmButton.interactable = rebirthSystem.CanBeginRebirth(selectedTierIndex);
            if (selectedTierIndex < 0)
            {
                guideText.text = "환생 가능한 레벨에 도달하면 환생 단계를 선택할 수 있습니다.";
            }
            else if (selectedTierIndex < rebirthSystem.MaxUnlockedTierIndex)
            {
                guideText.text = "낮은 단계 환생: 축복은 얻지만 획득 배율과 다음 상한은 증가하지 않습니다.";
            }
            else
            {
                guideText.text = "최대 단계 환생: 획득 배율이 증가하고 다음 환생 단계가 열립니다.";
            }
        }

        private void RefreshCandidates()
        {
            for (int i = 0; i < candidateButtons.Length; i++)
            {
                bool exists = i < blessingSystem.PendingCandidates.Count;
                candidateButtons[i].gameObject.SetActive(exists);
                if (!exists)
                {
                    continue;
                }

                BlessingDefinition blessing = blessingSystem.PendingCandidates[i];
                int ownedCount = blessingSystem.GetOwnedCount(blessing.Id);
                candidateLabels[i].text = $"[{blessing.Grade}] {blessing.DisplayName}\n{blessing.Description}\n보유 {ownedCount}개";
            }
        }

        private int FindHighestEligibleTier()
        {
            for (int i = rebirthSystem.MaxUnlockedTierIndex; i >= 0; i--)
            {
                if (rebirthSystem.CanBeginRebirth(i))
                {
                    return i;
                }
            }

            return -1;
        }

        private void OpenPanel()
        {
            rebirthPanel.SetActive(true);
            joystickInput.SetInputEnabled(false);
            selectedTierIndex = FindHighestEligibleTier();
            Refresh();
        }

        private void ClosePanel()
        {
            if (rebirthSystem.HasPendingRebirth)
            {
                return;
            }

            rebirthPanel.SetActive(false);
            joystickInput.SetInputEnabled(true);
        }

        private void SelectTier(int tierIndex)
        {
            if (!rebirthSystem.CanBeginRebirth(tierIndex))
            {
                return;
            }

            selectedTierIndex = tierIndex;
            Refresh();
        }

        private void ConfirmRebirth()
        {
            if (rebirthSystem.BeginRebirth(selectedTierIndex))
            {
                Refresh();
            }
        }

        private void ChooseBlessing(int candidateIndex)
        {
            if (!rebirthSystem.CompleteRebirth(candidateIndex, out RebirthResult result))
            {
                return;
            }

            Debug.Log($"환생 완료: [{result.Blessing.Grade}] {result.Blessing.DisplayName}, 획득 배율 x{result.RewardMultiplier:F1}", this);
            rebirthPanel.SetActive(false);
            joystickInput.SetInputEnabled(true);
            selectedTierIndex = -1;
            Refresh();
        }

        private void HandlePlayerStateChanged(PlayerSnapshot state)
        {
            Refresh();
        }

        private void OnDestroy()
        {
            if (playerState != null) playerState.StateChanged -= HandlePlayerStateChanged;
            if (rebirthSystem != null) rebirthSystem.StateChanged -= Refresh;
            if (blessingSystem != null) blessingSystem.StateChanged -= Refresh;
            if (openButton != null) openButton.onClick.RemoveListener(OpenPanel);
            if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
            if (confirmButton != null) confirmButton.onClick.RemoveListener(ConfirmRebirth);
        }

#if UNITY_EDITOR
        public void SetReferences(
            RectTransform targetGameArea,
            GameObject targetRebirthPanel,
            GameObject targetTierPanel,
            GameObject targetCandidatePanel,
            Text targetStatusText,
            Text targetGuideText,
            Button targetOpenButton,
            Button targetCloseButton,
            Button targetConfirmButton,
            Button[] targetTierButtons,
            Text[] targetTierLabels,
            Button[] targetCandidateButtons,
            Text[] targetCandidateLabels
        )
        {
            gameArea = targetGameArea;
            rebirthPanel = targetRebirthPanel;
            tierPanel = targetTierPanel;
            candidatePanel = targetCandidatePanel;
            statusText = targetStatusText;
            guideText = targetGuideText;
            openButton = targetOpenButton;
            closeButton = targetCloseButton;
            confirmButton = targetConfirmButton;
            tierButtons = targetTierButtons;
            tierLabels = targetTierLabels;
            candidateButtons = targetCandidateButtons;
            candidateLabels = targetCandidateLabels;
        }
#endif
    }
}
