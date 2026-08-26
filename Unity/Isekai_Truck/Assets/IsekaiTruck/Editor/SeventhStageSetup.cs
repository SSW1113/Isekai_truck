using System;
using IsekaiTruck.Camera;
using IsekaiTruck.Core;
using IsekaiTruck.Input;
using IsekaiTruck.Player;
using IsekaiTruck.Truck;
using IsekaiTruck.UI;
using IsekaiTruck.Upgrades;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class SeventhStageSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string FontFolder = "Assets/IsekaiTruck/Fonts";
        private const string CartoonFontAssetPath = FontFolder + "/CartoonHUD.asset";
        private const string CartoonFontSourceAssetPath = FontFolder + "/CartoonHUD.ttf";
        private const string TmpResourcesFolder = "Assets/TextMesh Pro/Resources";
        private const string TmpSettingsPath = TmpResourcesFolder + "/TMP Settings.asset";
        private const string TmpLeadingCharactersPath = TmpResourcesFolder + "/LineBreaking Leading Characters.txt";
        private const string TmpFollowingCharactersPath = TmpResourcesFolder + "/LineBreaking Following Characters.txt";
        private const string CartoonFontCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:/%+-!?레벨포인트업그레이드트럭남은속도크기최대닫기여신이지켜보고있습니다영혼도감";

        [MenuItem("Isekai Truck/Setup Game UI Stage")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이 모드를 종료한 뒤 실행해주세요.", "확인");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (gameManager == null || canvas == null)
            {
                throw new InvalidOperationException("Main 씬의 GameManager 또는 Game Canvas를 찾지 못했습니다.");
            }

            Transform existingUI = canvas.transform.Find("Game UI");
            if (existingUI != null)
            {
                MainHudLayoutSetup.DetachActionButtonsFromGameUI(canvas, existingUI);
                Object.DestroyImmediate(existingUI.gameObject);
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            TMP_FontAsset font = GetOrCreateCartoonFontAsset();
            GameUIController uiController = CreateUI(canvas.transform, font);
            MainHudLayoutSetup.ApplyToLoadedScene();
            Transform rebirthUI = canvas.transform.Find("Rebirth UI");
            if (rebirthUI != null)
            {
                rebirthUI.SetAsLastSibling();
            }
            RebirthFeatureSetup.ApplyPaletteToExistingUI(canvas.transform);

            gameManager.SetUISystem(uiController);
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이어 HUD와 트럭 업그레이드 UI를 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            GameUIController uiController = Object.FindFirstObjectByType<GameUIController>();
            JoystickInput joystickInput = Object.FindFirstObjectByType<JoystickInput>();
            CameraController cameraController = Object.FindFirstObjectByType<CameraController>();
            if (gameManager == null || uiController == null || joystickInput == null || cameraController == null)
            {
                throw new InvalidOperationException("게임 UI 씬 연결을 확인하지 못했습니다.");
            }

            Transform rebirthUI = uiController.transform.parent.Find("Rebirth UI");
            if (rebirthUI != null && rebirthUI.GetSiblingIndex() <= uiController.transform.GetSiblingIndex())
            {
                throw new InvalidOperationException("환생 UI가 메인 HUD 뒤에 가려져 있습니다.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            serializedGameManager.Update();
            if (serializedGameManager.FindProperty("gameUIController").objectReferenceValue != uiController)
            {
                throw new InvalidOperationException("GameManager의 GameUIController 참조가 비어 있습니다.");
            }

            SerializedObject serializedUI = new SerializedObject(uiController);
            serializedUI.Update();
            string[] requiredProperties =
            {
                "leftPanel", "gameArea", "rightPanel", "upgradePanel", "levelText", "expText", "expFill", "soulText", "speedText", "pointText",
                "upgradePointText", "speedLevelText", "sizeLevelText",
                "openButton", "closeButton", "speedButton", "sizeButton", "collectionButton",
                "collectionNotificationBadge", "upgradeAvailableIndicator",
                "levelFeedback", "soulFeedback", "upgradeFeedback", "speedFeedback", "speedHudView"
            };

            for (int i = 0; i < requiredProperties.Length; i++)
            {
                if (serializedUI.FindProperty(requiredProperties[i]).objectReferenceValue == null)
                {
                    throw new InvalidOperationException($"GameUIController 참조가 비어 있습니다: {requiredProperties[i]}");
                }
            }

            RectTransform leftPanel = (RectTransform)serializedUI.FindProperty("leftPanel").objectReferenceValue;
            RectTransform gameArea = (RectTransform)serializedUI.FindProperty("gameArea").objectReferenceValue;
            RectTransform rightPanel = (RectTransform)serializedUI.FindProperty("rightPanel").objectReferenceValue;
            if (leftPanel.Find("Growth HUD/Level Badge") != null || leftPanel.Find("Growth HUD/Growth Accent") != null ||
                leftPanel.Find("Growth HUD/EXP Bar/EXP Fill Area/EXP Fill") == null ||
                leftPanel.Find("Fuel Reserved Area") == null || leftPanel.Find("Upgrade CTA/Available Indicator") == null ||
                leftPanel.Find("Secondary Navigation/Collection Button/Icon") == null ||
                leftPanel.Find("Secondary Navigation/Collection Button/Label") == null ||
                leftPanel.Find("Secondary Navigation/Collection Button/NotificationBadge") == null ||
                rightPanel.Find("Goddess Area") != null || rightPanel.Find("Brand Logo") == null ||
                rightPanel.Find("System Navigation") != null || rightPanel.Find("Settings Button") != null ||
                rightPanel.Find("Soul Chip") == null || gameArea.Find("Speed HUD/Speed Text") == null ||
                rightPanel.Find("SpeedCard") != null || leftPanel.Find("LevelCard") != null || leftPanel.Find("ExpCard") != null)
            {
                throw new InvalidOperationException("좌측 패널, 중앙 게임 영역, 우측 패널의 UI 계층이 올바르지 않습니다.");
            }

            if (!HasSidePanelStyle(leftPanel) || !HasSidePanelStyle(rightPanel) ||
                leftPanel.Find("Energy Stripe 1") != null || rightPanel.Find("Energy Stripe 1") != null)
            {
                throw new InvalidOperationException("좌우 패널의 카툰 HUD 프레임이 올바르지 않습니다.");
            }

            Image leftPanelImage = leftPanel.GetComponent<Image>();
            Image rightPanelImage = rightPanel.GetComponent<Image>();
            Image growthImage = leftPanel.Find("Growth HUD").GetComponent<Image>();
            Image upgradeImage = leftPanel.Find("Upgrade CTA/Open Upgrade Button/Button Face").GetComponent<Image>();
            Image soulImage = rightPanel.Find("Soul Chip").GetComponent<Image>();
            Image speedImage = gameArea.Find("Speed HUD").GetComponent<Image>();
            if (!HudColorPalette.Matches(leftPanelImage.color, HudColorPalette.SidePanel) ||
                !HudColorPalette.Matches(rightPanelImage.color, HudColorPalette.SidePanel) ||
                !HudColorPalette.Matches(growthImage.color, HudColorPalette.Level) ||
                !HudColorPalette.Matches(upgradeImage.color, HudColorPalette.Upgrade) ||
                !HudColorPalette.Matches(soulImage.color, HudColorPalette.Soul) ||
                !HudColorPalette.Matches(speedImage.color, HudColorPalette.Speed))
            {
                throw new InvalidOperationException("HUD 색상 팔레트가 지정된 색상과 일치하지 않습니다.");
            }

            IsekaiTruck.Config.GameConfig config = AssetDatabase.LoadAssetAtPath<IsekaiTruck.Config.GameConfig>(
                "Assets/IsekaiTruck/Config/GameConfig.asset"
            );
            Rect wideViewport = CameraController.CalculateViewportRect(
                16f / 9f,
                config.Camera.ViewportAspect,
                config.Camera.ViewportHorizontalCenter
            );
            VerifyViewportLayout(uiController, leftPanel, gameArea, rightPanel, wideViewport, "1920x1080");

            if (!Mathf.Approximately(wideViewport.xMin, 0.19f) || !Mathf.Approximately(wideViewport.width, 0.60f) ||
                !Mathf.Approximately(1f - wideViewport.xMax, 0.21f))
            {
                throw new InvalidOperationException("1920x1080 HUD 비율이 Left 19% / Game 60% / Right 21%가 아닙니다.");
            }

            Rect compactViewport = CameraController.CalculateViewportRect(
                960f / 600f,
                config.Camera.ViewportAspect,
                config.Camera.ViewportHorizontalCenter
            );
            VerifyViewportLayout(uiController, leftPanel, gameArea, rightPanel, compactViewport, "960x600");

            Rect fourByThreeViewport = CameraController.CalculateViewportRect(
                4f / 3f,
                config.Camera.ViewportAspect,
                config.Camera.ViewportHorizontalCenter
            );
            VerifyViewportLayout(uiController, leftPanel, gameArea, rightPanel, fourByThreeViewport, "4:3");
            uiController.SetViewport(wideViewport);

            GameObject truckObject = new GameObject("UI Verification Truck");
            GameObject playerObject = new GameObject("UI Verification Player");
            GameObject upgradeObject = new GameObject("UI Verification Upgrade System");

            try
            {
                TruckController truck = truckObject.AddComponent<TruckController>();
                PlayerState player = playerObject.AddComponent<PlayerState>();
                TruckUpgradeSystem upgrades = upgradeObject.AddComponent<TruckUpgradeSystem>();
                truck.Initialize(config);
                player.Initialize(config);
                upgrades.Initialize(player, truck);
                uiController.Initialize(player, truck, upgrades, joystickInput, cameraController);

                Button openButton = (Button)serializedUI.FindProperty("openButton").objectReferenceValue;
                Button closeButton = (Button)serializedUI.FindProperty("closeButton").objectReferenceValue;
                Button speedButton = (Button)serializedUI.FindProperty("speedButton").objectReferenceValue;
                TMP_Text levelText = (TMP_Text)serializedUI.FindProperty("levelText").objectReferenceValue;
                TMP_Text soulText = (TMP_Text)serializedUI.FindProperty("soulText").objectReferenceValue;
                TMP_Text pointText = (TMP_Text)serializedUI.FindProperty("pointText").objectReferenceValue;
                TMP_Text speedText = (TMP_Text)serializedUI.FindProperty("speedText").objectReferenceValue;
                TMP_Text upgradePointText = (TMP_Text)serializedUI.FindProperty("upgradePointText").objectReferenceValue;
                TMP_Text speedLevelText = (TMP_Text)serializedUI.FindProperty("speedLevelText").objectReferenceValue;
                TMP_Text sizeLevelText = (TMP_Text)serializedUI.FindProperty("sizeLevelText").objectReferenceValue;
                Image expFill = (Image)serializedUI.FindProperty("expFill").objectReferenceValue;
                TMP_FontAsset cartoonFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CartoonFontAssetPath);

                GameObject upgradePanel = (GameObject)serializedUI.FindProperty("upgradePanel").objectReferenceValue;
                TMP_Text upgradeTitle = upgradePanel.transform.Find("Upgrade Box/Title").GetComponent<TMP_Text>();
                TMP_Text speedButtonLabel = speedButton.transform.Find("Button Face/Label").GetComponent<TMP_Text>();
                Button sizeButton = (Button)serializedUI.FindProperty("sizeButton").objectReferenceValue;
                TMP_Text sizeButtonLabel = sizeButton.transform.Find("Button Face/Label").GetComponent<TMP_Text>();
                if (!IsCentered(upgradeTitle) || !IsCentered(upgradePointText) || !IsCentered(speedLevelText) ||
                    !IsCentered(sizeLevelText) ||
                    !IsCentered(speedButtonLabel) || !IsCentered(sizeButtonLabel))
                {
                    throw new InvalidOperationException("업그레이드 팝업 글자가 가운데 정렬되지 않았습니다.");
                }

                Button collectionButton = (Button)serializedUI.FindProperty("collectionButton").objectReferenceValue;
                if (levelText.font != cartoonFont || openButton.GetComponent<CartoonButtonPressEffect>() == null ||
                    collectionButton.GetComponent<CartoonButtonPressEffect>() == null)
                {
                    throw new InvalidOperationException("카툰 HUD 폰트 또는 버튼 상호작용 효과가 연결되지 않았습니다.");
                }

                player.AddRewards(player.RequiredExp / 2);
                uiController.Refresh();
                float expectedExpRatio = (float)player.Exp / player.RequiredExp;
                if (!Mathf.Approximately(expFill.rectTransform.anchorMax.x, expectedExpRatio))
                {
                    throw new InvalidOperationException("EXP 게이지 너비가 현재 경험치 비율과 일치하지 않습니다.");
                }

                player.AddRewards(player.RequiredExp - player.Exp, 7);
                truck.UpdateTruck(Vector2.up, 1f / 60f);
                uiController.Refresh();
                openButton.onClick.Invoke();

                if (!uiController.IsUpgradePanelOpen || joystickInput.enabled)
                {
                    throw new InvalidOperationException("업그레이드 창이 열릴 때 조이스틱 입력이 차단되지 않았습니다.");
                }

                speedButton.onClick.Invoke();
                if (truck.GetStats().SpeedLevel != 1 || player.UpgradePoints != 0 || speedButton.interactable)
                {
                    throw new InvalidOperationException("속도 업그레이드 UI 동작이 기존 시스템과 일치하지 않습니다.");
                }

                if (levelText.text != "Lv. 2" || soulText.text != "7" || pointText.text != "포인트 0")
                {
                    throw new InvalidOperationException("플레이어 HUD 텍스트가 갱신되지 않았습니다.");
                }

                if (truck.CurrentSpeedPerSecond <= 0f || !speedText.text.EndsWith("km/h"))
                {
                    throw new InvalidOperationException("트럭 속도 또는 Speed HUD 표시 형식이 올바르지 않습니다.");
                }

                closeButton.onClick.Invoke();
                if (uiController.IsUpgradePanelOpen || !joystickInput.enabled)
                {
                    throw new InvalidOperationException("업그레이드 창을 닫은 뒤 조이스틱 입력이 복원되지 않았습니다.");
                }
            }
            finally
            {
                joystickInput.SetInputEnabled(true);
                Object.DestroyImmediate(upgradeObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(truckObject);
            }

            Debug.Log("Game UI stage verification passed.");
        }

        private static bool HasSidePanelStyle(RectTransform panel)
        {
            if (panel == null || panel.GetComponent<Outline>() == null)
            {
                return false;
            }

            Shadow[] effects = panel.GetComponents<Shadow>();
            for (int i = 0; i < effects.Length; i++)
            {
                if (effects[i].GetType() == typeof(Shadow))
                {
                    return false;
                }
            }

            Image image = panel.GetComponent<Image>();
            return image != null && image.sprite != null && image.type == Image.Type.Sliced;
        }

        private static bool IsCentered(TMP_Text text)
        {
            return text != null && text.alignment == TextAlignmentOptions.Center;
        }

        private static void VerifyViewportLayout(
            GameUIController uiController,
            RectTransform leftPanel,
            RectTransform gameArea,
            RectTransform rightPanel,
            Rect viewport,
            string resolutionLabel
        )
        {
            uiController.SetViewport(viewport);
            if (leftPanel.anchorMin != Vector2.zero || leftPanel.anchorMax != new Vector2(viewport.xMin, 1f) ||
                gameArea.anchorMin != viewport.min || gameArea.anchorMax != viewport.max ||
                rightPanel.anchorMin != new Vector2(viewport.xMax, 0f) || rightPanel.anchorMax != Vector2.one)
            {
                throw new InvalidOperationException($"{resolutionLabel} HUD가 카메라 Viewport에 맞춰지지 않았습니다.");
            }
        }

        private static TMP_FontAsset GetOrCreateCartoonFontAsset()
        {
            EnsureTmpSettings();

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(CartoonFontSourceAssetPath);
            if (sourceFont == null)
            {
                throw new InvalidOperationException($"카툰 HUD 원본 폰트를 찾지 못했습니다: {CartoonFontSourceAssetPath}");
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CartoonFontAssetPath);
            if (fontAsset != null)
            {
                AtlasPopulationMode populationMode = fontAsset.atlasPopulationMode;
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                if (!fontAsset.TryAddCharacters(CartoonFontCharacters, out string existingMissingCharacters))
                {
                    Debug.LogWarning($"카툰 HUD 폰트에서 생성하지 못한 문자가 있습니다: {existingMissingCharacters}");
                }
                fontAsset.atlasPopulationMode = populationMode;
                EditorUtility.SetDirty(fontAsset);
                EditorUtility.SetDirty(fontAsset.atlasTexture);
                TMP_Settings.defaultFontAsset = fontAsset;
                EditorUtility.SetDirty(TMP_Settings.instance);
                return fontAsset;
            }

            if (fontAsset != null)
            {
                AssetDatabase.DeleteAsset(CartoonFontAssetPath);
            }

            fontAsset = TMP_FontAsset.CreateFontAsset(
                sourceFont,
                90,
                9,
                GlyphRenderMode.SDFAA,
                1024,
                1024,
                AtlasPopulationMode.Dynamic,
                false
            );
            if (fontAsset == null)
            {
                throw new InvalidOperationException("카툰 HUD용 TMP Font Asset 생성에 실패했습니다.");
            }

            fontAsset.name = "CartoonHUD";
            if (!fontAsset.TryAddCharacters(CartoonFontCharacters, out string missingCharacters))
            {
                Debug.LogWarning($"카툰 HUD 폰트에서 생성하지 못한 문자가 있습니다: {missingCharacters}");
            }

            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            AssetDatabase.CreateAsset(fontAsset, CartoonFontAssetPath);

            Texture2D atlasTexture = fontAsset.atlasTexture;
            atlasTexture.name = "CartoonHUD Atlas";
            atlasTexture.filterMode = FilterMode.Bilinear;
            atlasTexture.anisoLevel = 0;
            AssetDatabase.AddObjectToAsset(atlasTexture, fontAsset);

            Material fontMaterial = fontAsset.material;
            fontMaterial.name = "CartoonHUD Material";
            AssetDatabase.AddObjectToAsset(fontMaterial, fontAsset);

            EditorUtility.SetDirty(fontAsset);
            TMP_Settings.defaultFontAsset = fontAsset;
            EditorUtility.SetDirty(TMP_Settings.instance);
            AssetDatabase.SaveAssets();
            return fontAsset;
        }

        private static void EnsureTmpSettings()
        {
            TMP_Settings existingSettings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath);
            if (existingSettings != null)
            {
                ConfigureTmpLineBreaking(existingSettings);
                return;
            }

            if (!AssetDatabase.IsValidFolder("Assets/TextMesh Pro"))
            {
                AssetDatabase.CreateFolder("Assets", "TextMesh Pro");
            }

            if (!AssetDatabase.IsValidFolder(TmpResourcesFolder))
            {
                AssetDatabase.CreateFolder("Assets/TextMesh Pro", "Resources");
            }

            TMP_Settings settings = ScriptableObject.CreateInstance<TMP_Settings>();
            settings.name = "TMP Settings";
            AssetDatabase.CreateAsset(settings, TmpSettingsPath);
            ConfigureTmpLineBreaking(settings);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (TMP_Settings.instance == null || Shader.Find("TextMeshPro/Mobile/Bitmap") == null)
            {
                throw new InvalidOperationException("TMP 비트맵 리소스 생성에 실패했습니다.");
            }
        }

        private static void ConfigureTmpLineBreaking(TMP_Settings settings)
        {
            TextAsset leadingCharacters = AssetDatabase.LoadAssetAtPath<TextAsset>(TmpLeadingCharactersPath);
            TextAsset followingCharacters = AssetDatabase.LoadAssetAtPath<TextAsset>(TmpFollowingCharactersPath);
            if (leadingCharacters == null || followingCharacters == null)
            {
                throw new InvalidOperationException("TMP 줄바꿈 문자 리소스를 찾지 못했습니다.");
            }

            SerializedObject serializedSettings = new SerializedObject(settings);
            serializedSettings.Update();
            serializedSettings.FindProperty("m_leadingCharacters").objectReferenceValue = leadingCharacters;
            serializedSettings.FindProperty("m_followingCharacters").objectReferenceValue = followingCharacters;
            serializedSettings.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static GameUIController CreateUI(Transform canvas, TMP_FontAsset font)
        {
            Color outlineColor = new Color(0.25f, 0.12f, 0.25f, 1f);
            Color sidePanelColor = HudColorPalette.SidePanel;
            Color levelColor = HudColorPalette.Level;
            Color levelDepth = HudColorPalette.LevelDepth;
            Color soulColor = HudColorPalette.Soul;
            Color soulDepth = HudColorPalette.SoulDepth;
            Color speedColor = HudColorPalette.Speed;
            Color speedDepth = HudColorPalette.SpeedDepth;
            Color creamColor = new Color(1f, 0.97f, 0.84f, 1f);
            Color yellowColor = HudColorPalette.Upgrade;
            GameObject uiObject = CreateUIObject("Game UI", canvas);
            Stretch(uiObject.GetComponent<RectTransform>());
            GameUIController controller = uiObject.AddComponent<GameUIController>();

            GameObject leftPanelObject = CreateSidePanel("LeftPanel", uiObject.transform, sidePanelColor, outlineColor);
            RectTransform leftPanel = leftPanelObject.GetComponent<RectTransform>();
            SetRect(leftPanel, Vector2.zero, new Vector2(0.19f, 1f), Vector2.zero, Vector2.zero);

            GameObject growthSection = CreatePanel("Growth HUD", leftPanel, levelColor);
            SetRect(growthSection.GetComponent<RectTransform>(), new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.91f), Vector2.zero, Vector2.zero);
            AddSoftOutline(growthSection, levelDepth);
            TMP_Text levelText = CreateText("Level Text", growthSection.transform, font, "Lv. 1", 38, TextAlignmentOptions.Center);
            SetRect(levelText.rectTransform, new Vector2(0.07f, 0.40f), new Vector2(0.48f, 0.92f), Vector2.zero, Vector2.zero);
            levelText.color = Color.white;
            levelText.fontStyle = FontStyles.Bold;
            AddTextOutline(levelText, outlineColor, 1.5f);
            UIFeedbackEffect levelFeedback = levelText.gameObject.AddComponent<UIFeedbackEffect>();
            levelFeedback.Configure(0.22f, 0.10f);

            TMP_Text expText = CreateText("EXP Text", growthSection.transform, font, "0 / 100", 17, TextAlignmentOptions.MidlineRight);
            SetRect(expText.rectTransform, new Vector2(0.50f, 0.48f), new Vector2(0.92f, 0.86f), Vector2.zero, Vector2.zero);
            expText.color = outlineColor;

            GameObject expBar = CreateProgressBar("EXP Bar", growthSection.transform, HudColorPalette.LevelTrack, outlineColor);
            SetRect(expBar.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.30f), Vector2.zero, Vector2.zero);
            GameObject expFillArea = CreateUIObject("EXP Fill Area", expBar.transform);
            StretchWithOffsets(expFillArea.GetComponent<RectTransform>(), 7f, 7f, 7f, 7f);
            GameObject expFillObject = CreatePanel("EXP Fill", expFillArea.transform, HudColorPalette.LevelFill);
            Image expFill = expFillObject.GetComponent<Image>();
            SetRect(expFill.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, Vector2.zero);
            expFill.rectTransform.pivot = new Vector2(0f, 0.5f);
            GameObject expShine = CreatePanel("EXP Shine", expFillObject.transform, new Color(1f, 1f, 0.72f, 0.50f));
            SetRect(expShine.GetComponent<RectTransform>(), new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.86f), Vector2.zero, Vector2.zero);
            expShine.GetComponent<Image>().raycastTarget = false;

            GameObject fuelReservedArea = CreateUIObject("Fuel Reserved Area", leftPanel);
            SetRect(fuelReservedArea.GetComponent<RectTransform>(), new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.16f), Vector2.zero, Vector2.zero);

            GameObject upgradeSection = CreateUIObject("Upgrade CTA", leftPanel);
            SetRect(upgradeSection.GetComponent<RectTransform>(), new Vector2(0.08f, 0.47f), new Vector2(0.92f, 0.69f), Vector2.zero, Vector2.zero);
            TMP_Text pointText = CreateText("Point Text", upgradeSection.transform, font, "포인트 0", 22, TextAlignmentOptions.Center);
            SetRect(pointText.rectTransform, new Vector2(0.18f, 0.70f), new Vector2(0.74f, 0.96f), Vector2.zero, Vector2.zero);
            pointText.color = outlineColor;
            pointText.fontStyle = FontStyles.Bold;
            Button openButton = CreateButton("Open Upgrade Button", upgradeSection.transform, font, "업그레이드", 22);
            SetRect(openButton.GetComponent<RectTransform>(), new Vector2(0.08f, 0.04f), new Vector2(0.92f, 0.66f), Vector2.zero, Vector2.zero);
            UIFeedbackEffect upgradeFeedback = openButton.gameObject.AddComponent<UIFeedbackEffect>();
            upgradeFeedback.Configure(0.20f, 0.035f);
            GameObject upgradeIndicator = CreateCirclePanel("Available Indicator", upgradeSection.transform, yellowColor);
            RectTransform upgradeIndicatorRect = upgradeIndicator.GetComponent<RectTransform>();
            SetRect(upgradeIndicatorRect, new Vector2(0.80f, 0.83f), new Vector2(0.80f, 0.83f), Vector2.zero, Vector2.zero);
            upgradeIndicatorRect.sizeDelta = new Vector2(34f, 34f);
            AddSoftOutline(upgradeIndicator, outlineColor);
            TMP_Text indicatorText = CreateText("Indicator Text", upgradeIndicator.transform, font, "!", 24, TextAlignmentOptions.Center);
            Stretch(indicatorText.rectTransform);
            indicatorText.color = outlineColor;
            upgradeIndicator.SetActive(false);

            GameObject secondaryNavigation = CreateUIObject("Secondary Navigation", leftPanel);
            SetRect(secondaryNavigation.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 0.12f), Vector2.zero, Vector2.zero);
            Button collectionButton = CreateCollectionButton(
                secondaryNavigation.transform,
                font,
                creamColor,
                outlineColor,
                yellowColor,
                out GameObject collectionNotificationBadge
            );

            GameObject gameAreaObject = CreateUIObject("Game Area UI", uiObject.transform);
            RectTransform gameArea = gameAreaObject.GetComponent<RectTransform>();
            SetRect(gameArea, new Vector2(0.19f, 0f), new Vector2(0.79f, 1f), Vector2.zero, Vector2.zero);

            CreateCenterFrame(gameArea, outlineColor, creamColor);

            GameObject speedHud = CreatePanel("Speed HUD", gameArea, speedColor);
            RectTransform speedHudRect = speedHud.GetComponent<RectTransform>();
            SetRect(speedHudRect, new Vector2(0.72f, 0.06f), new Vector2(0.94f, 0.125f), Vector2.zero, Vector2.zero);
            speedHudRect.localRotation = Quaternion.Euler(0f, 0f, -3f);
            AddSoftOutline(speedHud, speedDepth);
            CreateSpeedIcon(speedHud.transform, new Vector2(0.18f, 0.68f), creamColor);
            TMP_Text speedText = CreateText("Speed Text", speedHud.transform, font, "0 km/h", 25, TextAlignmentOptions.Center);
            SetRect(speedText.rectTransform, new Vector2(0.25f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);
            speedText.color = Color.white;
            speedText.fontStyle = FontStyles.Bold;
            AddTextOutline(speedText, outlineColor, 1.5f);
            UIFeedbackEffect speedFeedback = speedText.gameObject.AddComponent<UIFeedbackEffect>();
            speedFeedback.Configure(0.18f, 0.025f);
            SpeedHUDView speedHudView = speedHud.AddComponent<SpeedHUDView>();
            speedHudView.SetReferences(speedHudRect, speedText);
            speedHud.SetActive(true);

            GameObject upgradePanel = CreatePanel("Upgrade Panel", gameArea, new Color(0.25f, 0.15f, 0.28f, 0.58f));
            Stretch(upgradePanel.GetComponent<RectTransform>());

            GameObject upgradeBox = CreateCartoonPanel("Upgrade Box", upgradePanel.transform, new Color(0.94f, 0.83f, 0.65f, 0.99f), new Color(0.76f, 0.60f, 0.43f, 1f), outlineColor);
            RectTransform upgradeBoxRect = upgradeBox.GetComponent<RectTransform>();
            upgradeBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.pivot = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.sizeDelta = new Vector2(340f, 430f);
            upgradeBoxRect.anchoredPosition = Vector2.zero;
            ResponsivePanelFitter upgradeBoxFitter = upgradeBox.AddComponent<ResponsivePanelFitter>();
            upgradeBoxFitter.Configure(upgradeBoxRect.sizeDelta, 24f, 24f);

            TMP_Text title = CreateText("Title", upgradeBox.transform, font, "트럭 업그레이드", 30, TextAlignmentOptions.Center);
            SetTopRect(title.rectTransform, 18f, 48f, 16f);
            title.color = new Color(1f, 0.56f, 0.20f, 1f);
            title.fontStyle = FontStyles.Bold;
            AddTextOutline(title, outlineColor, 1.5f);

            TMP_Text upgradePointText = CreateText("Upgrade Point Text", upgradeBox.transform, font, "남은 포인트: 0", 22, TextAlignmentOptions.Center);
            SetTopRect(upgradePointText.rectTransform, 68f, 34f, 20f);
            upgradePointText.color = outlineColor;

            Button speedButton = CreateButton("Speed Upgrade Button", upgradeBox.transform, font, "속도 업그레이드", 24);
            SetTopRect(speedButton.GetComponent<RectTransform>(), 115f, 58f, 22f);
            TMP_Text speedLevelText = CreateText("Speed Level", speedButton.transform, font, "Lv.0", 17, TextAlignmentOptions.Center);
            SetRect(speedLevelText.rectTransform, new Vector2(0.12f, 0.02f), new Vector2(0.88f, 0.38f), Vector2.zero, Vector2.zero);
            speedLevelText.raycastTarget = false;
            speedLevelText.fontStyle = FontStyles.Bold;

            TMP_Text speedButtonLabel = speedButton.transform.Find("Button Face/Label").GetComponent<TMP_Text>();
            SetRect(speedButtonLabel.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

            Button sizeButton = CreateButton("Size Upgrade Button", upgradeBox.transform, font, "크기 업그레이드", 24);
            SetTopRect(sizeButton.GetComponent<RectTransform>(), 225f, 58f, 22f);
            TMP_Text sizeLevelText = CreateText("Size Level", sizeButton.transform, font, "Lv.0", 17, TextAlignmentOptions.Center);
            SetRect(sizeLevelText.rectTransform, new Vector2(0.12f, 0.02f), new Vector2(0.88f, 0.38f), Vector2.zero, Vector2.zero);
            sizeLevelText.raycastTarget = false;
            sizeLevelText.fontStyle = FontStyles.Bold;

            TMP_Text sizeButtonLabel = sizeButton.transform.Find("Button Face/Label").GetComponent<TMP_Text>();
            SetRect(sizeButtonLabel.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

            Button closeButton = CreateButton("Close Button", upgradeBox.transform, font, "닫기", 22);
            SetTopRect(closeButton.GetComponent<RectTransform>(), 342f, 54f, 22f);

            GameObject rightPanelObject = CreateSidePanel("RightPanel", uiObject.transform, sidePanelColor, outlineColor);
            RectTransform rightPanel = rightPanelObject.GetComponent<RectTransform>();
            SetRect(rightPanel, new Vector2(0.79f, 0f), Vector2.one, Vector2.zero, Vector2.zero);

            MainHudLayoutSetup.CreateBrandLogo(rightPanel);

            GameObject soulSection = CreatePanel("Soul Chip", rightPanel, soulColor);
            SetRect(soulSection.GetComponent<RectTransform>(), new Vector2(0.18f, 0.22f), new Vector2(0.82f, 0.31f), Vector2.zero, Vector2.zero);
            AddSoftOutline(soulSection, soulDepth);
            CreateSoulIcon(soulSection.transform, new Vector2(0.20f, 0.50f), creamColor, outlineColor);
            TMP_Text soulLabel = CreateText("Soul Label", soulSection.transform, font, "영혼", 17, TextAlignmentOptions.MidlineLeft);
            SetRect(soulLabel.rectTransform, new Vector2(0.32f, 0.12f), new Vector2(0.58f, 0.88f), Vector2.zero, Vector2.zero);
            soulLabel.color = outlineColor;
            TMP_Text soulText = CreateText("Soul Text", soulSection.transform, font, "0", 28, TextAlignmentOptions.MidlineRight);
            SetRect(soulText.rectTransform, new Vector2(0.52f, 0.10f), new Vector2(0.90f, 0.90f), Vector2.zero, Vector2.zero);
            soulText.color = Color.white;
            soulText.fontStyle = FontStyles.Bold;
            AddTextOutline(soulText, outlineColor, 1.5f);
            UIFeedbackEffect soulFeedback = soulText.gameObject.AddComponent<UIFeedbackEffect>();
            soulFeedback.Configure(0.22f, 0.08f);

            controller.SetReferences(
                leftPanel,
                gameArea,
                rightPanel,
                upgradePanel,
                levelText,
                expText,
                expFill,
                soulText,
                speedText,
                pointText,
                upgradePointText,
                speedLevelText,
                sizeLevelText,
                openButton,
                closeButton,
                speedButton,
                sizeButton,
                collectionButton,
                collectionNotificationBadge,
                upgradeIndicator,
                levelFeedback,
                soulFeedback,
                upgradeFeedback,
                speedFeedback,
                speedHudView
            );

            MonsterCollectionUIController existingCollectionUI = canvas.GetComponentInChildren<MonsterCollectionUIController>(true);
            existingCollectionUI?.SetOpenButton(collectionButton);

            upgradePanel.SetActive(false);
            return controller;
        }

        private static GameObject CreateUIObject(string name, Transform parent)
        {
            GameObject gameObject = new GameObject(name, typeof(RectTransform));
            gameObject.transform.SetParent(parent, false);
            return gameObject;
        }

        private static GameObject CreatePanel(string name, Transform parent, Color color)
        {
            GameObject panel = CreateUIObject(name, parent);
            Image image = panel.AddComponent<Image>();
            image.color = color;
            image.sprite = GetRoundedSprite();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = 0.5f;
            return panel;
        }

        private static GameObject CreateSidePanel(string name, Transform parent, Color color, Color outlineColor)
        {
            GameObject panel = CreatePanel(name, parent, color);
            AddSoftOutline(panel, outlineColor);
            return panel;
        }

        private static void AddSoftOutline(GameObject target, Color color)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, Mathf.Min(color.a, 0.72f));
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static GameObject CreateCirclePanel(string name, Transform parent, Color color)
        {
            GameObject panel = CreatePanel(name, parent, color);
            Image image = panel.GetComponent<Image>();
            image.sprite = GetCircleSprite();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return panel;
        }

        private static Button CreateCollectionButton(
            Transform parent,
            TMP_FontAsset font,
            Color faceColor,
            Color outlineColor,
            Color notificationColor,
            out GameObject notificationBadge
        )
        {
            GameObject buttonObject = CreatePanel("Collection Button", parent, faceColor);
            RectTransform buttonRect = buttonObject.GetComponent<RectTransform>();
            SetRect(buttonRect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero);
            buttonRect.sizeDelta = new Vector2(200f, 58f);
            buttonRect.anchoredPosition = new Vector2(0f, 47f);
            AddSoftOutline(buttonObject, outlineColor);

            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();
            ConfigureSoftButtonColors(
                button,
                new Color(1f, 0.98f, 0.91f, 1f),
                new Color(0.94f, 0.88f, 0.76f, 1f),
                new Color(0.72f, 0.68f, 0.60f, 0.72f)
            );

            GameObject icon = CreateUIObject("Icon", buttonObject.transform);
            RectTransform iconRect = icon.GetComponent<RectTransform>();
            SetRect(iconRect, new Vector2(0.22f, 0.5f), new Vector2(0.22f, 0.5f), Vector2.zero, Vector2.zero);
            iconRect.sizeDelta = new Vector2(34f, 28f);
            CreateBookIcon(icon.transform, faceColor, outlineColor);

            TMP_Text label = CreateText("Label", buttonObject.transform, font, "도감", 20, TextAlignmentOptions.Center);
            SetRect(label.rectTransform, new Vector2(0.34f, 0.10f), new Vector2(0.88f, 0.90f), Vector2.zero, Vector2.zero);
            label.color = outlineColor;
            label.fontStyle = FontStyles.Bold;

            CartoonButtonPressEffect interaction = buttonObject.AddComponent<CartoonButtonPressEffect>();
            interaction.Configure(buttonRect, iconRect, 1.03f, 0.97f, 1f, 1.04f);

            notificationBadge = CreateCirclePanel("NotificationBadge", buttonObject.transform, notificationColor);
            RectTransform badgeRect = notificationBadge.GetComponent<RectTransform>();
            SetRect(badgeRect, new Vector2(0.90f, 0.88f), new Vector2(0.90f, 0.88f), Vector2.zero, Vector2.zero);
            badgeRect.sizeDelta = new Vector2(20f, 20f);
            AddSoftOutline(notificationBadge, outlineColor);
            TMP_Text badgeText = CreateText("Badge Text", notificationBadge.transform, font, "!", 15, TextAlignmentOptions.Center);
            Stretch(badgeText.rectTransform);
            badgeText.color = outlineColor;
            notificationBadge.SetActive(false);
            return button;
        }

        private static void CreateBookIcon(Transform parent, Color pageColor, Color outlineColor)
        {
            GameObject leftPage = CreatePanel("Left Page", parent, outlineColor);
            SetRect(leftPage.GetComponent<RectTransform>(), new Vector2(0f, 0.08f), new Vector2(0.48f, 0.92f), Vector2.zero, Vector2.zero);
            GameObject leftFace = CreatePanel("Page Face", leftPage.transform, pageColor);
            StretchWithOffsets(leftFace.GetComponent<RectTransform>(), 2f, 2f, 2f, 2f);
            leftFace.GetComponent<Image>().raycastTarget = false;

            GameObject rightPage = CreatePanel("Right Page", parent, outlineColor);
            SetRect(rightPage.GetComponent<RectTransform>(), new Vector2(0.52f, 0.08f), new Vector2(1f, 0.92f), Vector2.zero, Vector2.zero);
            GameObject rightFace = CreatePanel("Page Face", rightPage.transform, pageColor);
            StretchWithOffsets(rightFace.GetComponent<RectTransform>(), 2f, 2f, 2f, 2f);
            rightFace.GetComponent<Image>().raycastTarget = false;

            leftPage.GetComponent<Image>().raycastTarget = false;
            rightPage.GetComponent<Image>().raycastTarget = false;
        }

        private static void ConfigureSoftButtonColors(Button button, Color highlighted, Color pressed, Color disabled)
        {
            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = highlighted;
            colors.pressedColor = pressed;
            colors.selectedColor = highlighted;
            colors.disabledColor = disabled;
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;
        }

        private static GameObject CreateCartoonPanel(string name, Transform parent, Color faceColor, Color depthColor, Color outlineColor)
        {
            GameObject panel = CreatePanel(name, parent, depthColor);

            GameObject face = CreatePanel("Panel Face", panel.transform, faceColor);
            StretchWithOffsets(face.GetComponent<RectTransform>(), 4f, 4f, 8f, 4f);
            face.GetComponent<Image>().raycastTarget = false;

            GameObject topShine = CreatePanel("Top Shine", panel.transform, new Color(1f, 1f, 1f, 0.24f));
            SetRect(topShine.GetComponent<RectTransform>(), new Vector2(0.10f, 0.82f), new Vector2(0.90f, 0.91f), Vector2.zero, Vector2.zero);
            topShine.GetComponent<Image>().raycastTarget = false;
            return panel;
        }

        private static GameObject CreateProgressBar(string name, Transform parent, Color faceColor, Color outlineColor)
        {
            GameObject progressBar = CreatePanel(name, parent, outlineColor);

            GameObject face = CreatePanel("Bar Face", progressBar.transform, faceColor);
            Image faceImage = face.GetComponent<Image>();
            StretchWithOffsets(face.GetComponent<RectTransform>(), 4f, 4f, 4f, 4f);
            faceImage.raycastTarget = false;
            return progressBar;
        }

        private static void CreateCartoonDecorations(Transform parent)
        {
            CreateSparkle(parent, new Vector2(0.15f, 0.48f), new Color(1f, 0.69f, 0.24f, 0.48f), 18f);
            CreateSparkle(parent, new Vector2(0.84f, 0.35f), new Color(0.55f, 0.76f, 0.94f, 0.42f), 13f);

            for (int i = 0; i < 3; i++)
            {
                GameObject roadMark = CreatePanel($"Tire Mark {i + 1}", parent, new Color(0.33f, 0.22f, 0.30f, 0.12f));
                SetRect(roadMark.GetComponent<RectTransform>(), new Vector2(0.66f + i * 0.06f, 0.42f - i * 0.035f), new Vector2(0.73f + i * 0.06f, 0.435f - i * 0.035f), Vector2.zero, Vector2.zero);
                roadMark.transform.localRotation = Quaternion.Euler(0f, 0f, -18f);
                roadMark.GetComponent<Image>().raycastTarget = false;
            }
        }

        private static void CreateSparkle(Transform parent, Vector2 anchor, Color color, float size)
        {
            GameObject sparkle = CreateUIObject("Sparkle Decoration", parent);
            RectTransform sparkleRect = sparkle.GetComponent<RectTransform>();
            SetRect(sparkleRect, anchor, anchor, Vector2.zero, Vector2.zero);
            sparkleRect.sizeDelta = new Vector2(size, size);

            GameObject vertical = CreatePanel("Vertical", sparkle.transform, color);
            SetRect(vertical.GetComponent<RectTransform>(), new Vector2(0.39f, 0f), new Vector2(0.61f, 1f), Vector2.zero, Vector2.zero);
            vertical.GetComponent<Image>().raycastTarget = false;

            GameObject horizontal = CreatePanel("Horizontal", sparkle.transform, color);
            SetRect(horizontal.GetComponent<RectTransform>(), new Vector2(0f, 0.39f), new Vector2(1f, 0.61f), Vector2.zero, Vector2.zero);
            horizontal.GetComponent<Image>().raycastTarget = false;
        }

        private static void CreateCenterFrame(Transform parent, Color outlineColor, Color accentColor)
        {
            GameObject leftEdge = CreatePanel("Center Frame Left", parent, outlineColor);
            SetRect(leftEdge.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(6f, 0f));
            leftEdge.GetComponent<Image>().raycastTarget = false;

            GameObject rightEdge = CreatePanel("Center Frame Right", parent, outlineColor);
            SetRect(rightEdge.GetComponent<RectTransform>(), new Vector2(1f, 0f), Vector2.one, new Vector2(-6f, 0f), Vector2.zero);
            rightEdge.GetComponent<Image>().raycastTarget = false;

            GameObject leftAccent = CreatePanel("Center Accent Left", parent, accentColor);
            SetRect(leftAccent.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0f, 1f), new Vector2(6f, 0f), new Vector2(9f, 0f));
            leftAccent.GetComponent<Image>().raycastTarget = false;

            GameObject rightAccent = CreatePanel("Center Accent Right", parent, accentColor);
            SetRect(rightAccent.GetComponent<RectTransform>(), new Vector2(1f, 0f), Vector2.one, new Vector2(-9f, 0f), new Vector2(-6f, 0f));
            rightAccent.GetComponent<Image>().raycastTarget = false;
        }

        private static GameObject CreateSpeechBubble(string name, Transform parent, Color faceColor, Color outlineColor)
        {
            GameObject bubble = CreatePanel(name, parent, faceColor);
            Outline outline = bubble.AddComponent<Outline>();
            outline.effectColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.78f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            GameObject tail = CreatePanel("Bubble Tail", bubble.transform, faceColor);
            SetRect(tail.GetComponent<RectTransform>(), new Vector2(0.16f, 0f), new Vector2(0.16f, 0f), new Vector2(-7f, -7f), new Vector2(7f, 7f));
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Outline tailOutline = tail.AddComponent<Outline>();
            tailOutline.effectColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.72f);
            tailOutline.effectDistance = new Vector2(1f, -1f);
            tail.GetComponent<Image>().raycastTarget = false;
            return bubble;
        }

        private static void CreateSoulIcon(Transform parent, Vector2 anchor, Color faceColor, Color outlineColor)
        {
            GameObject orb = CreateCirclePanel("Soul Icon", parent, faceColor);
            RectTransform orbRect = orb.GetComponent<RectTransform>();
            SetRect(orbRect, anchor, anchor, Vector2.zero, Vector2.zero);
            orbRect.sizeDelta = new Vector2(28f, 28f);
            Outline outline = orb.AddComponent<Outline>();
            outline.effectColor = new Color(outlineColor.r, outlineColor.g, outlineColor.b, 0.78f);
            outline.effectDistance = new Vector2(1.25f, -1.25f);

            GameObject tail = CreatePanel("Soul Tail", orb.transform, faceColor);
            SetRect(tail.GetComponent<RectTransform>(), new Vector2(0.56f, 0.08f), new Vector2(1.02f, 0.34f), Vector2.zero, Vector2.zero);
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, -24f);
            tail.GetComponent<Image>().raycastTarget = false;
        }

        private static void CreateSpeedIcon(Transform parent, Vector2 anchor, Color color)
        {
            for (int i = 0; i < 3; i++)
            {
                GameObject windLine = CreatePanel($"Wind Line {i + 1}", parent, color);
                RectTransform rectTransform = windLine.GetComponent<RectTransform>();
                Vector2 lineAnchor = anchor + new Vector2(i * 0.018f, -i * 0.065f);
                SetRect(rectTransform, lineAnchor, lineAnchor, Vector2.zero, Vector2.zero);
                rectTransform.sizeDelta = new Vector2(28f - i * 5f, 6f);
                windLine.GetComponent<Image>().raycastTarget = false;
            }
        }

        private static Sprite GetRoundedSprite()
        {
            return AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        }

        private static Sprite GetCircleSprite()
        {
            Sprite circleSprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            return circleSprite != null ? circleSprite : GetRoundedSprite();
        }

        private static void AddTextOutline(TMP_Text text, Color color, float distance)
        {
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, Mathf.Min(color.a, 0.82f));
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private static TMP_Text CreateText(string name, Transform parent, TMP_FontAsset font, string value, int fontSize, TextAlignmentOptions alignment)
        {
            GameObject textObject = CreateUIObject(name, parent);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = new Color(0.25f, 0.12f, 0.25f, 1f);
            text.raycastTarget = false;
            text.fontStyle = FontStyles.Normal;
            text.characterSpacing = 0f;
            text.enableAutoSizing = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, TMP_FontAsset font, string label, int fontSize)
        {
            Color outlineColor = HudColorPalette.UpgradeDepth;
            GameObject buttonObject = CreatePanel(name, parent, outlineColor);
            Button button = buttonObject.AddComponent<Button>();

            GameObject face = CreatePanel("Button Face", buttonObject.transform, HudColorPalette.Upgrade);
            StretchWithOffsets(face.GetComponent<RectTransform>(), 3f, 3f, 7f, 3f);
            Image faceImage = face.GetComponent<Image>();
            button.targetGraphic = faceImage;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
            colors.pressedColor = new Color(0.94f, 0.74f, 0.56f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            GameObject highlight = CreatePanel("Button Highlight", face.transform, new Color(1f, 0.96f, 0.72f, 0.42f));
            SetRect(highlight.GetComponent<RectTransform>(), new Vector2(0.16f, 0.73f), new Vector2(0.84f, 0.84f), Vector2.zero, Vector2.zero);
            highlight.GetComponent<Image>().raycastTarget = false;

            TMP_Text text = CreateText("Label", face.transform, font, label, fontSize, TextAlignmentOptions.Center);
            text.color = Color.white;
            text.fontStyle = FontStyles.Bold;
            StretchWithOffsets(text.rectTransform, 10f, 10f, 0f, 0f);
            AddTextOutline(text, outlineColor, 1.5f);

            CartoonButtonPressEffect pressEffect = buttonObject.AddComponent<CartoonButtonPressEffect>();
            pressEffect.Configure(face.GetComponent<RectTransform>(), null, 1.04f, 0.97f, 1.5f);
            return button;
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void StretchWithOffsets(RectTransform rectTransform, float left, float right, float bottom, float top)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one, new Vector2(left, bottom), new Vector2(-right, -top));
        }

        private static void SetTopRect(RectTransform rectTransform, float top, float height, float horizontalMargin)
        {
            SetRect(
                rectTransform,
                new Vector2(0f, 1f),
                Vector2.one,
                new Vector2(horizontalMargin, -top - height),
                new Vector2(-horizontalMargin, -top)
            );
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }
    }
}
