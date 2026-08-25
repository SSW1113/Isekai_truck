using System;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Truck;
using IsekaiTruck.UI;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class MainHudLayoutSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private static readonly Color Cream = new Color32(0xF4, 0xE7, 0xC3, 0xFF);
        private static readonly Color DarkInk = new Color32(0x4C, 0x38, 0x45, 0xFF);
        private static readonly Color SoftWhite = new Color32(0xFF, 0xFB, 0xF2, 0xFF);
        private static readonly Color PortraitBackground = new Color32(0xB5, 0xD7, 0xDD, 0xFF);

        [MenuItem("Isekai Truck/Apply Main HUD Layout")]
        public static void Setup()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ApplyToLoadedScene();
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "중앙 HUD와 사이드 패널 레이아웃을 적용했습니다.", "확인");
            }
        }

        internal static void ApplyToLoadedScene()
        {
            GameUIController gameUI = Object.FindFirstObjectByType<GameUIController>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (gameUI == null || canvas == null)
            {
                throw new InvalidOperationException("Main HUD layout dependencies were not found.");
            }

            SerializedObject serializedGameUI = new SerializedObject(gameUI);
            RectTransform leftPanel = (RectTransform)serializedGameUI.FindProperty("leftPanel").objectReferenceValue;
            RectTransform gameArea = (RectTransform)serializedGameUI.FindProperty("gameArea").objectReferenceValue;
            RectTransform rightPanel = (RectTransform)serializedGameUI.FindProperty("rightPanel").objectReferenceValue;
            if (leftPanel == null || gameArea == null || rightPanel == null)
            {
                throw new InvalidOperationException("Main HUD panel references are missing.");
            }

            HideCentralHud(gameArea, canvas.transform);
            ShowWantedHud(gameUI, gameArea);
            ReserveWantedHudSpaceForEnemyWarnings();
            MoveHealthUI(canvas, leftPanel);
            MoveActionButtons(canvas, rightPanel);
            ApplyVisualPolish(gameUI, canvas, leftPanel, gameArea, rightPanel);
        }

        internal static void DetachActionButtonsFromGameUI(Canvas canvas, Transform gameUI)
        {
            BlessingInventoryUIController blessingUI = canvas.GetComponentInChildren<BlessingInventoryUIController>(true);
            DetachActionButton(blessingUI, gameUI);

            WorldTravelUIController worldTravelUI = canvas.GetComponentInChildren<WorldTravelUIController>(true);
            DetachActionButton(worldTravelUI, gameUI);
        }

        private static void DetachActionButton(MonoBehaviour controller, Transform gameUI)
        {
            if (controller == null)
            {
                return;
            }

            SerializedObject serializedController = new SerializedObject(controller);
            Button button = (Button)serializedController.FindProperty("openButton").objectReferenceValue;
            RectTransform featureGameArea = (RectTransform)serializedController.FindProperty("gameArea").objectReferenceValue;
            if (button != null && featureGameArea != null && button.transform.IsChildOf(gameUI))
            {
                button.transform.SetParent(featureGameArea, false);
            }
        }

        private static void HideCentralHud(RectTransform gameArea, Transform canvas)
        {
            SetInactive(gameArea.Find("Speed HUD"));

            Transform legacyGameArea = gameArea.Find("Game Area UI");
            if (legacyGameArea != null)
            {
                SetInactive(legacyGameArea.Find("Player HUD"));
            }

            SetInactive(canvas.Find("Blessing Inventory UI/Blessing Game Area/Active Blessing Slots"));
            SetInactive(canvas.Find("World Travel UI/World Travel Game Area/Current World Panel"));
        }

        private static void ShowWantedHud(GameUIController gameUI, RectTransform gameArea)
        {
            RectTransform wantedRect = gameArea.Find("Wanted Level UI") as RectTransform;
            if (wantedRect == null)
            {
                wantedRect = gameArea.Find("Game Area UI/Wanted Level UI") as RectTransform;
            }

            if (wantedRect == null)
            {
                return;
            }

            if (wantedRect.parent != gameArea)
            {
                wantedRect.SetParent(gameArea, false);
            }

            wantedRect.anchorMin = new Vector2(0.5f, 1f);
            wantedRect.anchorMax = new Vector2(0.5f, 1f);
            wantedRect.pivot = new Vector2(0.5f, 1f);
            wantedRect.anchoredPosition = new Vector2(0f, -22f);
            wantedRect.sizeDelta = new Vector2(470f, 110f);
            wantedRect.localRotation = Quaternion.identity;
            wantedRect.localScale = Vector3.one;
            wantedRect.gameObject.SetActive(true);

            GameObject upgradePanel = (GameObject)new SerializedObject(gameUI).FindProperty("upgradePanel").objectReferenceValue;
            if (upgradePanel != null && upgradePanel.transform.parent == gameArea)
            {
                wantedRect.SetSiblingIndex(upgradePanel.transform.GetSiblingIndex());
            }
        }

        private static void ReserveWantedHudSpaceForEnemyWarnings()
        {
            EnemyWarningUIController warningUI = Object.FindFirstObjectByType<EnemyWarningUIController>(FindObjectsInactive.Include);
            if (warningUI == null)
            {
                return;
            }

            SerializedObject serializedWarning = new SerializedObject(warningUI);
            SerializedProperty topEdgePadding = serializedWarning.FindProperty("topEdgePadding");
            if (topEdgePadding != null)
            {
                topEdgePadding.floatValue = 180f;
                serializedWarning.ApplyModifiedPropertiesWithoutUndo();
            }
        }

        private static void MoveHealthUI(Canvas canvas, RectTransform leftPanel)
        {
            TruckHealthUIController healthUI = GetReferencedHealthUI();
            if (healthUI == null)
            {
                TruckHealthUIController[] healthUIs = canvas.GetComponentsInChildren<TruckHealthUIController>(true);
                if (healthUIs.Length > 0)
                {
                    healthUI = healthUIs[0];
                }
            }

            if (healthUI == null)
            {
                return;
            }

            RectTransform healthRect = healthUI.GetComponent<RectTransform>();
            healthRect.SetParent(leftPanel, false);
            SetRect(healthRect, new Vector2(0.08f, 0.37f), new Vector2(0.92f, 0.45f));

            Image panelImage = healthUI.GetComponent<Image>();
            panelImage.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            panelImage.type = Image.Type.Sliced;
            panelImage.color = HudColorPalette.LevelTrack;
            panelImage.raycastTarget = false;
            AddPanelDepth(healthUI.gameObject, HudColorPalette.LevelDepth);

            SerializedObject serializedHealthUI = new SerializedObject(healthUI);
            Text healthText = (Text)serializedHealthUI.FindProperty("healthText").objectReferenceValue;
            if (healthText == null)
            {
                return;
            }

            healthText.fontStyle = FontStyle.Bold;
            healthText.fontSize = 23;
            healthText.alignment = TextAnchor.MiddleCenter;
            healthText.color = Color.white;
            healthText.supportRichText = true;
            healthText.resizeTextForBestFit = true;
            healthText.resizeTextMinSize = 16;
            healthText.resizeTextMaxSize = 23;
            healthText.raycastTarget = false;
            healthText.text = "체력  <color=#E990B8>♥</color> <color=#E990B8>♥</color> <color=#E990B8>♥</color>";
            UIFeedbackEffect feedback = healthText.GetComponent<UIFeedbackEffect>();
            if (feedback == null)
            {
                feedback = healthText.gameObject.AddComponent<UIFeedbackEffect>();
            }
            feedback.Configure(0.18f, 0.06f);
            serializedHealthUI.FindProperty("feedbackEffect").objectReferenceValue = feedback;
            serializedHealthUI.ApplyModifiedPropertiesWithoutUndo();
            RectTransform textRect = healthText.rectTransform;
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(10f, 4f);
            textRect.offsetMax = new Vector2(-10f, -4f);
        }

        private static TruckHealthUIController GetReferencedHealthUI()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                return null;
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            return (TruckHealthUIController)serializedGameManager.FindProperty("truckHealthUIController").objectReferenceValue;
        }

        private static void MoveActionButtons(Canvas canvas, RectTransform rightPanel)
        {
            BlessingInventoryUIController blessingUI = canvas.GetComponentInChildren<BlessingInventoryUIController>(true);
            if (blessingUI != null)
            {
                SerializedObject serializedBlessingUI = new SerializedObject(blessingUI);
                Button blessingButton = (Button)serializedBlessingUI.FindProperty("openButton").objectReferenceValue;
                MoveActionButton(
                    blessingButton,
                    rightPanel,
                    new Vector2(0.18f, 0.075f),
                    new Vector2(0.82f, 0.13f),
                    HudColorPalette.Upgrade,
                    HudColorPalette.UpgradeDepth
                );
            }

            WorldTravelUIController worldTravelUI = canvas.GetComponentInChildren<WorldTravelUIController>(true);
            if (worldTravelUI != null)
            {
                SerializedObject serializedWorldTravelUI = new SerializedObject(worldTravelUI);
                Button worldTravelButton = (Button)serializedWorldTravelUI.FindProperty("openButton").objectReferenceValue;
                MoveActionButton(
                    worldTravelButton,
                    rightPanel,
                    new Vector2(0.18f, 0.015f),
                    new Vector2(0.82f, 0.07f),
                    HudColorPalette.Soul,
                    HudColorPalette.SoulDepth
                );
            }
        }

        private static void MoveActionButton(
            Button button,
            RectTransform rightPanel,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Color faceColor,
            Color depthColor
        )
        {
            if (button == null)
            {
                return;
            }

            Transform staleButton = rightPanel.Find(button.name);
            if (staleButton != null && staleButton != button.transform)
            {
                Object.DestroyImmediate(staleButton.gameObject);
            }

            RectTransform buttonRect = button.GetComponent<RectTransform>();
            buttonRect.SetParent(rightPanel, false);
            SetRect(buttonRect, anchorMin, anchorMax);

            Image image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = faceColor;
            button.targetGraphic = image;
            AddPanelDepth(button.gameObject, depthColor);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.88f);
            colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.58f, 0.55f, 0.57f, 0.68f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            CartoonButtonPressEffect interaction = button.GetComponent<CartoonButtonPressEffect>();
            if (interaction == null)
            {
                interaction = button.gameObject.AddComponent<CartoonButtonPressEffect>();
            }
            interaction.Configure(buttonRect, null, 1.025f, 0.97f, 1.2f);

            Text label = button.GetComponentInChildren<Text>(true);
            if (label == null)
            {
                return;
            }

            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 12;
            label.resizeTextMaxSize = 20;
            label.raycastTarget = false;
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = new Vector2(10f, 4f);
            labelRect.offsetMax = new Vector2(-10f, -4f);
            AddTextOutline(label, depthColor);
        }

        private static void ApplyVisualPolish(
            GameUIController gameUI,
            Canvas canvas,
            RectTransform leftPanel,
            RectTransform gameArea,
            RectTransform rightPanel
        )
        {
            PolishSidePanel(leftPanel);
            PolishSidePanel(rightPanel);
            PolishGrowthHud(gameUI, leftPanel);
            PolishGoddessArea(rightPanel);
            PolishSoulChip(gameUI, rightPanel);
            PolishHiddenSpeedHud(gameArea);
            HideCenterFrame(gameArea);

            SerializedObject serializedGameUI = new SerializedObject(gameUI);
            PolishButton(
                (Button)serializedGameUI.FindProperty("openButton").objectReferenceValue,
                HudColorPalette.Upgrade,
                HudColorPalette.UpgradeDepth,
                DarkInk
            );
            PolishButton(
                (Button)serializedGameUI.FindProperty("closeButton").objectReferenceValue,
                HudColorPalette.Upgrade,
                HudColorPalette.UpgradeDepth,
                DarkInk
            );
            PolishButton(
                (Button)serializedGameUI.FindProperty("speedButton").objectReferenceValue,
                HudColorPalette.Upgrade,
                HudColorPalette.UpgradeDepth,
                DarkInk
            );
            PolishButton(
                (Button)serializedGameUI.FindProperty("sizeButton").objectReferenceValue,
                HudColorPalette.Upgrade,
                HudColorPalette.UpgradeDepth,
                DarkInk
            );
            PolishButton(
                (Button)serializedGameUI.FindProperty("collectionButton").objectReferenceValue,
                Cream,
                HudColorPalette.UpgradeDepth,
                DarkInk
            );
            PolishButton(
                (Button)serializedGameUI.FindProperty("settingsButton").objectReferenceValue,
                Cream,
                HudColorPalette.UpgradeDepth,
                DarkInk
            );

            ConfigureFeedback(
                (UIFeedbackEffect)serializedGameUI.FindProperty("levelFeedback").objectReferenceValue,
                0.18f,
                0.06f
            );
            ConfigureFeedback(
                (UIFeedbackEffect)serializedGameUI.FindProperty("soulFeedback").objectReferenceValue,
                0.18f,
                0.05f
            );
            ConfigureFeedback(
                (UIFeedbackEffect)serializedGameUI.FindProperty("upgradeFeedback").objectReferenceValue,
                0.18f,
                0.035f
            );

            BlessingInventoryUIController blessingUI = canvas.GetComponentInChildren<BlessingInventoryUIController>(true);
            if (blessingUI != null)
            {
                Button blessingButton = (Button)new SerializedObject(blessingUI).FindProperty("openButton").objectReferenceValue;
                PolishButton(blessingButton, HudColorPalette.Upgrade, HudColorPalette.UpgradeDepth, DarkInk);
            }

            WorldTravelUIController worldTravelUI = canvas.GetComponentInChildren<WorldTravelUIController>(true);
            if (worldTravelUI != null)
            {
                Button worldTravelButton = (Button)new SerializedObject(worldTravelUI).FindProperty("openButton").objectReferenceValue;
                PolishButton(worldTravelButton, HudColorPalette.Soul, HudColorPalette.SoulDepth, SoftWhite);
            }

            RebirthUIController rebirthUI = canvas.GetComponentInChildren<RebirthUIController>(true);
            if (rebirthUI != null)
            {
                SerializedObject serializedRebirthUI = new SerializedObject(rebirthUI);
                Button rebirthButton = (Button)serializedRebirthUI.FindProperty("openButton").objectReferenceValue;
                PolishButton(rebirthButton, HudColorPalette.Soul, HudColorPalette.SoulDepth, SoftWhite);
                PolishRebirthIndicator(serializedRebirthUI);
                SoftenVisualEffects(rebirthButton.transform);
            }

            SoftenVisualEffects(gameUI.transform);
        }

        private static void PolishSidePanel(RectTransform panel)
        {
            Image image = panel.GetComponent<Image>();
            if (image != null)
            {
                image.color = HudColorPalette.SidePanel;
            }
        }

        private static void PolishGrowthHud(GameUIController gameUI, RectTransform leftPanel)
        {
            Transform growthHud = leftPanel.Find("Growth HUD");
            if (growthHud == null)
            {
                return;
            }

            Image growthImage = growthHud.GetComponent<Image>();
            growthImage.color = HudColorPalette.Level;

            Transform expBar = growthHud.Find("EXP Bar");
            if (expBar != null)
            {
                Image borderImage = expBar.GetComponent<Image>();
                borderImage.color = new Color(
                    HudColorPalette.LevelDepth.r,
                    HudColorPalette.LevelDepth.g,
                    HudColorPalette.LevelDepth.b,
                    0.28f
                );

                Transform barFace = expBar.Find("Bar Face");
                if (barFace != null)
                {
                    barFace.GetComponent<Image>().color = HudColorPalette.LevelTrack;
                    RectTransform faceRect = (RectTransform)barFace;
                    faceRect.offsetMin = new Vector2(2f, 2f);
                    faceRect.offsetMax = new Vector2(-2f, -2f);
                }
            }

            SerializedObject serializedGameUI = new SerializedObject(gameUI);
            TMP_Text levelText = (TMP_Text)serializedGameUI.FindProperty("levelText").objectReferenceValue;
            TMP_Text expText = (TMP_Text)serializedGameUI.FindProperty("expText").objectReferenceValue;
            Image expFill = (Image)serializedGameUI.FindProperty("expFill").objectReferenceValue;
            levelText.color = SoftWhite;
            expText.color = DarkInk;
            expFill.color = HudColorPalette.LevelFill;
        }

        private static void PolishGoddessArea(RectTransform rightPanel)
        {
            RectTransform goddessArea = rightPanel.Find("Goddess Area") as RectTransform;
            if (goddessArea == null)
            {
                return;
            }

            SetRect(goddessArea, new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.89f));
            RectTransform portraitFrame = goddessArea.Find("Portrait Frame") as RectTransform;
            if (portraitFrame != null)
            {
                portraitFrame.sizeDelta = new Vector2(270f, 270f);
                Image frameImage = portraitFrame.GetComponent<Image>();
                frameImage.color = Cream;

                Transform background = portraitFrame.Find("Portrait Background");
                if (background != null)
                {
                    background.GetComponent<Image>().color = PortraitBackground;
                }
            }

            RectTransform speechBubble = goddessArea.Find("Speech Bubble") as RectTransform;
            if (speechBubble != null)
            {
                speechBubble.sizeDelta = new Vector2(270f, 78f);
            }
        }

        private static void PolishSoulChip(GameUIController gameUI, RectTransform rightPanel)
        {
            RectTransform soulChip = rightPanel.Find("Soul Chip") as RectTransform;
            if (soulChip == null)
            {
                return;
            }

            SetRect(soulChip, new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.30f));
            soulChip.GetComponent<Image>().color = HudColorPalette.Soul;

            RectTransform soulIcon = soulChip.Find("Soul Icon") as RectTransform;
            if (soulIcon != null)
            {
                soulIcon.sizeDelta = new Vector2(24f, 24f);
                soulIcon.GetComponent<Image>().color = Cream;
            }

            TMP_Text soulLabel = soulChip.Find("Soul Label")?.GetComponent<TMP_Text>();
            if (soulLabel != null)
            {
                soulLabel.color = Cream;
            }

            SerializedObject serializedGameUI = new SerializedObject(gameUI);
            TMP_Text soulText = (TMP_Text)serializedGameUI.FindProperty("soulText").objectReferenceValue;
            soulText.color = SoftWhite;
        }

        private static void HideCenterFrame(RectTransform gameArea)
        {
            SetInactive(gameArea.Find("Center Frame Left"));
            SetInactive(gameArea.Find("Center Frame Right"));
            SetInactive(gameArea.Find("Center Accent Left"));
            SetInactive(gameArea.Find("Center Accent Right"));
        }

        private static void PolishHiddenSpeedHud(RectTransform gameArea)
        {
            Image speedImage = gameArea.Find("Speed HUD")?.GetComponent<Image>();
            if (speedImage != null)
            {
                speedImage.color = HudColorPalette.Speed;
            }
        }

        private static void PolishButton(Button button, Color faceColor, Color depthColor, Color textColor)
        {
            if (button == null)
            {
                return;
            }

            Image targetImage = button.targetGraphic as Image;
            Image rootImage = button.GetComponent<Image>();
            if (targetImage == null)
            {
                targetImage = rootImage;
                button.targetGraphic = targetImage;
            }

            Sprite roundedSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            if (rootImage != null)
            {
                rootImage.sprite = roundedSprite;
                rootImage.type = Image.Type.Sliced;
                rootImage.color = rootImage == targetImage
                    ? faceColor
                    : new Color(depthColor.r, depthColor.g, depthColor.b, 0.28f);
            }

            if (targetImage != null)
            {
                targetImage.sprite = roundedSprite;
                targetImage.type = Image.Type.Sliced;
                targetImage.color = faceColor;

                if (targetImage.transform != button.transform)
                {
                    RectTransform faceRect = targetImage.rectTransform;
                    faceRect.offsetMin = new Vector2(1.5f, 3f);
                    faceRect.offsetMax = new Vector2(-1.5f, -1.5f);
                }

                Transform highlight = targetImage.transform.Find("Button Highlight");
                if (highlight != null)
                {
                    Image highlightImage = highlight.GetComponent<Image>();
                    highlightImage.color = new Color(1f, 1f, 1f, 0.16f);
                }
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1.06f, 1.06f, 1.06f, 1f);
            colors.pressedColor = new Color(0.94f, 0.94f, 0.94f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.68f, 0.66f, 0.67f, 0.70f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            TMP_Text[] tmpLabels = button.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < tmpLabels.Length; i++)
            {
                tmpLabels[i].color = textColor;
            }

            Text[] labels = button.GetComponentsInChildren<Text>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                labels[i].color = textColor;
            }

            PolishButtonInteraction(button);
        }

        private static void PolishButtonInteraction(Button button)
        {
            CartoonButtonPressEffect interaction = button.GetComponent<CartoonButtonPressEffect>();
            if (interaction == null)
            {
                interaction = button.gameObject.AddComponent<CartoonButtonPressEffect>();
                interaction.Configure((RectTransform)button.transform, null, 1.02f, 0.985f, 0.4f);
            }

            SerializedObject serializedInteraction = new SerializedObject(interaction);
            serializedInteraction.FindProperty("hoverScale").floatValue = 1.02f;
            serializedInteraction.FindProperty("pressedScale").floatValue = 0.985f;
            serializedInteraction.FindProperty("hoverOffset").floatValue = 0.4f;
            serializedInteraction.FindProperty("transitionSpeed").floatValue = 20f;
            SerializedProperty accentScale = serializedInteraction.FindProperty("accentHoverScale");
            SerializedProperty accentRotation = serializedInteraction.FindProperty("accentHoverRotation");
            accentScale.floatValue = Mathf.Min(accentScale.floatValue, 1.03f);
            accentRotation.floatValue = Mathf.Clamp(accentRotation.floatValue, -3f, 3f);
            serializedInteraction.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void PolishRebirthIndicator(SerializedObject serializedRebirthUI)
        {
            GameObject indicator = (GameObject)serializedRebirthUI.FindProperty("availabilityIndicator").objectReferenceValue;
            if (indicator == null)
            {
                return;
            }

            Image image = indicator.GetComponent<Image>();
            if (image != null)
            {
                image.color = new Color32(0xF0, 0xCD, 0x70, 0xFF);
            }
        }

        private static void ConfigureFeedback(UIFeedbackEffect feedback, float duration, float strength)
        {
            if (feedback != null)
            {
                feedback.Configure(duration, strength);
            }
        }

        private static void SoftenVisualEffects(Transform root)
        {
            Outline[] outlines = root.GetComponentsInChildren<Outline>(true);
            for (int i = 0; i < outlines.Length; i++)
            {
                if (IsWantedHudElement(outlines[i].transform))
                {
                    continue;
                }

                bool isText = outlines[i].GetComponent<Text>() != null || outlines[i].GetComponent<TMP_Text>() != null;
                Color color = outlines[i].effectColor;
                color.a = Mathf.Min(color.a, isText ? 0.28f : 0.16f);
                outlines[i].effectColor = color;
                float distance = isText ? 0.75f : 0.5f;
                outlines[i].effectDistance = new Vector2(distance, -distance);
            }

            Shadow[] shadows = root.GetComponentsInChildren<Shadow>(true);
            for (int i = 0; i < shadows.Length; i++)
            {
                if (IsWantedHudElement(shadows[i].transform))
                {
                    continue;
                }

                if (shadows[i].GetType() != typeof(Shadow))
                {
                    continue;
                }

                Color color = shadows[i].effectColor;
                color.a = Mathf.Min(color.a, 0.10f);
                shadows[i].effectColor = color;
                shadows[i].effectDistance = new Vector2(0f, -1.25f);
            }
        }

        private static bool IsWantedHudElement(Transform target)
        {
            Transform current = target;
            while (current != null)
            {
                if (current.name == "Wanted Level UI")
                {
                    return true;
                }

                current = current.parent;
            }

            return false;
        }

        private static void AddPanelDepth(GameObject target, Color depthColor)
        {
            Outline outline = target.GetComponent<Outline>();
            if (outline == null)
            {
                outline = target.AddComponent<Outline>();
            }
            outline.effectColor = new Color(depthColor.r, depthColor.g, depthColor.b, 0.16f);
            outline.effectDistance = new Vector2(0.5f, -0.5f);

            Shadow shadow = null;
            Shadow[] shadows = target.GetComponents<Shadow>();
            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i].GetType() == typeof(Shadow))
                {
                    shadow = shadows[i];
                    break;
                }
            }

            if (shadow == null)
            {
                shadow = target.AddComponent<Shadow>();
            }
            shadow.effectColor = new Color(depthColor.r, depthColor.g, depthColor.b, 0.10f);
            shadow.effectDistance = new Vector2(0f, -1.25f);
        }

        private static void AddTextOutline(Text text, Color color)
        {
            Outline outline = text.GetComponent<Outline>();
            if (outline == null)
            {
                outline = text.gameObject.AddComponent<Outline>();
            }
            outline.effectColor = new Color(color.r, color.g, color.b, 0.82f);
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void SetInactive(Transform target)
        {
            if (target != null)
            {
                target.gameObject.SetActive(false);
            }
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
            rectTransform.localRotation = Quaternion.identity;
            rectTransform.localScale = Vector3.one;
        }

        private static void Verify()
        {
            GameUIController gameUI = Object.FindFirstObjectByType<GameUIController>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            SerializedObject serializedGameUI = new SerializedObject(gameUI);
            RectTransform leftPanel = (RectTransform)serializedGameUI.FindProperty("leftPanel").objectReferenceValue;
            RectTransform gameArea = (RectTransform)serializedGameUI.FindProperty("gameArea").objectReferenceValue;
            RectTransform rightPanel = (RectTransform)serializedGameUI.FindProperty("rightPanel").objectReferenceValue;

            Transform legacyGameArea = gameArea.Find("Game Area UI");
            if (gameArea.Find("Speed HUD").gameObject.activeSelf ||
                legacyGameArea != null && legacyGameArea.Find("Player HUD").gameObject.activeSelf)
            {
                throw new InvalidOperationException("Central HUD is still visible.");
            }

            VerifyWantedHud(gameArea);

            TruckHealthUIController healthUI = GetReferencedHealthUI();
            if (healthUI == null || healthUI.transform.parent != leftPanel)
            {
                throw new InvalidOperationException("Truck health UI is not under the left panel.");
            }

            BlessingInventoryUIController blessingUI = canvas.GetComponentInChildren<BlessingInventoryUIController>(true);
            WorldTravelUIController worldTravelUI = canvas.GetComponentInChildren<WorldTravelUIController>(true);
            Button blessingButton = (Button)new SerializedObject(blessingUI).FindProperty("openButton").objectReferenceValue;
            Button worldTravelButton = (Button)new SerializedObject(worldTravelUI).FindProperty("openButton").objectReferenceValue;
            if (blessingButton.transform.parent != rightPanel || worldTravelButton.transform.parent != rightPanel)
            {
                throw new InvalidOperationException("Side panel action buttons are not under the right panel.");
            }

            VerifyVisualPolish(gameArea, leftPanel, rightPanel, healthUI, blessingButton, worldTravelButton);
            VerifyHealthHearts();
            Debug.Log("Main HUD layout verification passed.");
        }

        private static void VerifyWantedHud(RectTransform gameArea)
        {
            RectTransform wantedRect = gameArea.Find("Wanted Level UI") as RectTransform;
            if (wantedRect == null ||
                !wantedRect.gameObject.activeSelf ||
                wantedRect.parent != gameArea ||
                wantedRect.anchorMin != new Vector2(0.5f, 1f) ||
                wantedRect.anchorMax != new Vector2(0.5f, 1f) ||
                wantedRect.sizeDelta != new Vector2(470f, 110f))
            {
                throw new InvalidOperationException("Wanted HUD is not visible at the top center of the game area.");
            }

            EnemyWarningUIController warningUI = Object.FindFirstObjectByType<EnemyWarningUIController>(FindObjectsInactive.Include);
            if (warningUI == null)
            {
                throw new InvalidOperationException("Enemy warning UI is missing.");
            }

            SerializedObject serializedWarning = new SerializedObject(warningUI);
            if (serializedWarning.FindProperty("topEdgePadding").floatValue < 180f)
            {
                throw new InvalidOperationException("Enemy warnings do not reserve enough space for the wanted HUD.");
            }
        }

        private static void VerifyVisualPolish(
            RectTransform gameArea,
            RectTransform leftPanel,
            RectTransform rightPanel,
            TruckHealthUIController healthUI,
            Button blessingButton,
            Button worldTravelButton
        )
        {
            if (gameArea.Find("Center Frame Left").gameObject.activeSelf ||
                gameArea.Find("Center Frame Right").gameObject.activeSelf)
            {
                throw new InvalidOperationException("Strong center frame separators are still visible.");
            }

            Outline leftOutline = leftPanel.GetComponent<Outline>();
            Outline rightOutline = rightPanel.GetComponent<Outline>();
            if (leftOutline == null || rightOutline == null || leftOutline.effectColor.a > 0.2f || rightOutline.effectColor.a > 0.2f)
            {
                throw new InvalidOperationException("Side panel outlines were not softened.");
            }

            RectTransform portraitFrame = rightPanel.Find("Goddess Area/Portrait Frame") as RectTransform;
            if (portraitFrame == null || portraitFrame.sizeDelta != new Vector2(270f, 270f))
            {
                throw new InvalidOperationException("Goddess portrait proportions were not polished.");
            }

            SerializedObject serializedHealthUI = new SerializedObject(healthUI);
            if (serializedHealthUI.FindProperty("feedbackEffect").objectReferenceValue == null)
            {
                throw new InvalidOperationException("Truck health feedback effect is missing.");
            }

            VerifyButtonInteraction(blessingButton);
            VerifyButtonInteraction(worldTravelButton);
        }

        private static void VerifyButtonInteraction(Button button)
        {
            CartoonButtonPressEffect interaction = button.GetComponent<CartoonButtonPressEffect>();
            if (interaction == null)
            {
                throw new InvalidOperationException($"Button interaction is missing: {button.name}");
            }

            SerializedObject serializedInteraction = new SerializedObject(interaction);
            float hoverScale = serializedInteraction.FindProperty("hoverScale").floatValue;
            float pressedScale = serializedInteraction.FindProperty("pressedScale").floatValue;
            if (!Mathf.Approximately(hoverScale, 1.02f) || !Mathf.Approximately(pressedScale, 0.985f))
            {
                throw new InvalidOperationException($"Button interaction is not using the polished values: {button.name}");
            }
        }

        private static void VerifyHealthHearts()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/IsekaiTruck/Config/GameConfig.asset");
            GameObject truckObject = new GameObject("HUD Health Verification Truck");
            GameObject uiObject = new GameObject("HUD Health Verification UI", typeof(RectTransform), typeof(Text));
            try
            {
                TruckDamageFlash damageFlash = truckObject.AddComponent<TruckDamageFlash>();
                TruckHealthController health = truckObject.AddComponent<TruckHealthController>();
                TruckHealthUIController healthUI = uiObject.AddComponent<TruckHealthUIController>();
                Text healthText = uiObject.GetComponent<Text>();
                UIFeedbackEffect feedback = uiObject.AddComponent<UIFeedbackEffect>();
                healthUI.SetReferences(healthText, feedback);
                health.Initialize(config, damageFlash);
                healthUI.Initialize(health);

                if (CountCharacter(healthText.text, '♥') != config.Truck.MaxHealth || CountCharacter(healthText.text, '♡') != 0)
                {
                    throw new InvalidOperationException("Full truck health hearts are not rendered correctly.");
                }

                health.TryTakeDamage(1);
                if (CountCharacter(healthText.text, '♥') != config.Truck.MaxHealth - 1 || CountCharacter(healthText.text, '♡') != 1)
                {
                    throw new InvalidOperationException("Damaged truck health hearts are not rendered correctly.");
                }
            }
            finally
            {
                Object.DestroyImmediate(uiObject);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static int CountCharacter(string value, char target)
        {
            int count = 0;
            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == target)
                {
                    count++;
                }
            }

            return count;
        }
    }
}
