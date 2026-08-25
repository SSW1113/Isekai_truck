using IsekaiTruck.Blessings;
using IsekaiTruck.Camera;
using IsekaiTruck.Input;
using IsekaiTruck.Player;
using UnityEngine;
using UnityEngine.UI;

namespace IsekaiTruck.UI
{
    [DisallowMultipleComponent]
    public sealed class BlessingInventoryUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform gameArea;
        [SerializeField] private GameObject inventoryPanel;
        [SerializeField] private Text selectionText;
        [SerializeField] private Button openButton;
        [SerializeField] private Button closeButton;
        [SerializeField] private Button equipButton;
        [SerializeField] private Button unequipButton;
        [SerializeField] private Button dismantleButton;
        [SerializeField] private Button[] slotButtons;
        [SerializeField] private Text[] slotLabels;
        [SerializeField] private Text[] activeSlotLabels;
        [SerializeField] private Button[] inventoryButtons;
        [SerializeField] private Text[] inventoryLabels;

        private BlessingSystem blessingSystem;
        private BlessingLoadoutSystem loadoutSystem;
        private BlessingDismantleSystem dismantleSystem;
        private BlessingEffectSystem effectSystem;
        private JoystickInput joystickInput;
        private string[] inventoryBlessingIds;
        private int selectedSlotIndex;
        private string selectedBlessingId;

        public bool IsPanelOpen => inventoryPanel != null && inventoryPanel.activeSelf;

        public void Initialize(
            BlessingSystem blessings,
            BlessingLoadoutSystem loadout,
            BlessingDismantleSystem dismantle,
            BlessingEffectSystem effects,
            JoystickInput input,
            CameraController cameraController
        )
        {
            blessingSystem = blessings;
            loadoutSystem = loadout;
            dismantleSystem = dismantle;
            effectSystem = effects;
            joystickInput = input;
            inventoryBlessingIds = new string[inventoryButtons.Length];

            blessingSystem.StateChanged += Refresh;
            loadoutSystem.StateChanged += Refresh;
            effectSystem.StateChanged += Refresh;
            openButton.onClick.AddListener(OpenPanel);
            closeButton.onClick.AddListener(ClosePanel);
            equipButton.onClick.AddListener(EquipSelected);
            unequipButton.onClick.AddListener(UnequipSelectedSlot);
            dismantleButton.onClick.AddListener(DismantleSelected);

            for (int i = 0; i < slotButtons.Length; i++)
            {
                int slotIndex = i;
                slotButtons[i].onClick.AddListener(() => SelectSlot(slotIndex));
            }

            for (int i = 0; i < inventoryButtons.Length; i++)
            {
                int inventoryIndex = i;
                inventoryButtons[i].onClick.AddListener(() => SelectInventory(inventoryIndex));
            }

            inventoryPanel.SetActive(false);
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

        public void RefreshRuntime()
        {
            if (loadoutSystem == null)
            {
                return;
            }

            for (int i = 0; i < activeSlotLabels.Length; i++)
            {
                BlessingDefinition definition = loadoutSystem.GetEquipped(i);
                if (definition == null)
                {
                    activeSlotLabels[i].text = $"{i + 1}  비어 있음";
                    continue;
                }

                if (definition.ActivationType == BlessingActivationType.Passive)
                {
                    activeSlotLabels[i].text = $"{i + 1}  [{definition.Grade}] {definition.DisplayName} (패시브)";
                    continue;
                }

                float remaining = effectSystem.GetRemainingDuration(i);
                string state = remaining > 0f ? $"{remaining:F1}초" : "사용 가능";
                activeSlotLabels[i].text = $"{i + 1}  [{definition.Grade}] {definition.DisplayName} ({state})";
            }
        }

        public void Refresh()
        {
            if (blessingSystem == null)
            {
                return;
            }

            if (!string.IsNullOrEmpty(selectedBlessingId) && blessingSystem.GetOwnedCount(selectedBlessingId) <= 0)
            {
                selectedBlessingId = null;
            }

            for (int i = 0; i < slotButtons.Length; i++)
            {
                BlessingDefinition definition = loadoutSystem.GetEquipped(i);
                string selected = i == selectedSlotIndex ? "▶ " : string.Empty;
                slotLabels[i].text = definition == null
                    ? $"{selected}슬롯 {i + 1}\n비어 있음"
                    : $"{selected}슬롯 {i + 1}\n[{definition.Grade}] {definition.DisplayName}";
            }

            int inventoryIndex = 0;
            for (int i = 0; i < blessingSystem.Definitions.Count && inventoryIndex < inventoryButtons.Length; i++)
            {
                BlessingDefinition definition = blessingSystem.Definitions[i];
                int ownedCount = definition == null ? 0 : blessingSystem.GetOwnedCount(definition.Id);
                if (definition == null || ownedCount <= 0)
                {
                    continue;
                }

                inventoryBlessingIds[inventoryIndex] = definition.Id;
                inventoryButtons[inventoryIndex].gameObject.SetActive(true);
                int equippedCount = loadoutSystem.GetEquippedCount(definition.Id);
                string selected = definition.Id == selectedBlessingId ? "▶ " : string.Empty;
                inventoryLabels[inventoryIndex].text = $"{selected}[{definition.Grade}] {definition.DisplayName}\n보유 {ownedCount} / 장착 {equippedCount}";
                inventoryIndex++;
            }

            for (int i = inventoryIndex; i < inventoryButtons.Length; i++)
            {
                inventoryBlessingIds[i] = null;
                inventoryButtons[i].gameObject.SetActive(false);
            }

            BlessingDefinition selectedBlessing = blessingSystem.FindDefinition(selectedBlessingId);
            if (selectedBlessing == null)
            {
                selectionText.text = "장착하거나 분해할 축복을 선택하세요.";
                equipButton.interactable = false;
                dismantleButton.interactable = false;
            }
            else
            {
                int availableCount = dismantleSystem.GetAvailableCount(selectedBlessing.Id);
                int soul = dismantleSystem.GetDismantleSoul(selectedBlessing.Grade);
                selectionText.text = $"[{selectedBlessing.Grade}] {selectedBlessing.DisplayName}\n{selectedBlessing.Description}\n분해 가능 {availableCount}개 / 영혼 +{soul}";
                BlessingDefinition current = loadoutSystem.GetEquipped(selectedSlotIndex);
                equipButton.interactable = current == selectedBlessing || loadoutSystem.GetEquippedCount(selectedBlessing.Id) < blessingSystem.GetOwnedCount(selectedBlessing.Id);
                dismantleButton.interactable = availableCount > 0;
            }

            unequipButton.interactable = loadoutSystem.GetEquipped(selectedSlotIndex) != null;
            RefreshRuntime();
        }

        private void OpenPanel()
        {
            inventoryPanel.SetActive(true);
            joystickInput.SetInputEnabled(false);
            Refresh();
        }

        private void ClosePanel()
        {
            inventoryPanel.SetActive(false);
            joystickInput.SetInputEnabled(true);
        }

        private void SelectSlot(int slotIndex)
        {
            selectedSlotIndex = slotIndex;
            BlessingDefinition equipped = loadoutSystem.GetEquipped(slotIndex);
            if (equipped != null)
            {
                selectedBlessingId = equipped.Id;
            }

            Refresh();
        }

        private void SelectInventory(int inventoryIndex)
        {
            if (inventoryIndex < 0 || inventoryIndex >= inventoryBlessingIds.Length)
            {
                return;
            }

            BlessingDefinition definition = blessingSystem.FindDefinition(inventoryBlessingIds[inventoryIndex]);
            if (definition != null && blessingSystem.GetOwnedCount(definition.Id) > 0)
            {
                selectedBlessingId = definition.Id;
                Refresh();
            }
        }

        private void EquipSelected()
        {
            if (!string.IsNullOrEmpty(selectedBlessingId))
            {
                loadoutSystem.TryEquip(selectedSlotIndex, selectedBlessingId);
            }
        }

        private void UnequipSelectedSlot()
        {
            loadoutSystem.Unequip(selectedSlotIndex);
        }

        private void DismantleSelected()
        {
            if (!string.IsNullOrEmpty(selectedBlessingId))
            {
                dismantleSystem.TryDismantle(selectedBlessingId, out BlessingDismantleResult result);
            }
        }

        private void OnDestroy()
        {
            if (blessingSystem != null) blessingSystem.StateChanged -= Refresh;
            if (loadoutSystem != null) loadoutSystem.StateChanged -= Refresh;
            if (effectSystem != null) effectSystem.StateChanged -= Refresh;
            if (openButton != null) openButton.onClick.RemoveListener(OpenPanel);
            if (closeButton != null) closeButton.onClick.RemoveListener(ClosePanel);
            if (equipButton != null) equipButton.onClick.RemoveListener(EquipSelected);
            if (unequipButton != null) unequipButton.onClick.RemoveListener(UnequipSelectedSlot);
            if (dismantleButton != null) dismantleButton.onClick.RemoveListener(DismantleSelected);
        }

#if UNITY_EDITOR
        public void SetReferences(
            RectTransform targetGameArea,
            GameObject targetInventoryPanel,
            Text targetSelectionText,
            Button targetOpenButton,
            Button targetCloseButton,
            Button targetEquipButton,
            Button targetUnequipButton,
            Button targetDismantleButton,
            Button[] targetSlotButtons,
            Text[] targetSlotLabels,
            Text[] targetActiveSlotLabels,
            Button[] targetInventoryButtons,
            Text[] targetInventoryLabels
        )
        {
            gameArea = targetGameArea;
            inventoryPanel = targetInventoryPanel;
            selectionText = targetSelectionText;
            openButton = targetOpenButton;
            closeButton = targetCloseButton;
            equipButton = targetEquipButton;
            unequipButton = targetUnequipButton;
            dismantleButton = targetDismantleButton;
            slotButtons = targetSlotButtons;
            slotLabels = targetSlotLabels;
            activeSlotLabels = targetActiveSlotLabels;
            inventoryButtons = targetInventoryButtons;
            inventoryLabels = targetInventoryLabels;
        }
#endif
    }
}
