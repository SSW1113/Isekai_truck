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
        private const string CartoonFontCharacters = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789 .,:/%+-!?레벨포인트업그레이드트럭남은속도크기최대닫기여신이지켜보고있습니다영혼";

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
                Object.DestroyImmediate(existingUI.gameObject);
            }

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1080f, 1920f);
            scaler.matchWidthOrHeight = 0.5f;

            TMP_FontAsset font = GetOrCreateCartoonFontAsset();
            GameUIController uiController = CreateUI(canvas.transform, font);
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
                "upgradePointText", "speedLevelText", "sizeLevelText", "speedStatText", "sizeStatText",
                "openButton", "closeButton", "speedButton", "sizeButton"
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
            if (leftPanel.Find("LevelCard") == null || leftPanel.Find("ExpCard") == null ||
                leftPanel.Find("Fuel Reserved Area") == null || leftPanel.Find("UpgradeCard") == null ||
                rightPanel.Find("GoddessCard/Goddess Silhouette/Head") == null ||
                rightPanel.Find("GoddessCard/Speech Bubble/Goddess Message") == null ||
                rightPanel.Find("SoulCard") == null || rightPanel.Find("SpeedCard") == null ||
                GameObject.Find("Player HUD") != null)
            {
                throw new InvalidOperationException("좌측 패널, 중앙 게임 영역, 우측 패널의 UI 계층이 올바르지 않습니다.");
            }

            if (!HasCartoonPanelStyle(leftPanel) || !HasCartoonPanelStyle(rightPanel) ||
                leftPanel.Find("Energy Stripe 1") != null || rightPanel.Find("Energy Stripe 1") != null)
            {
                throw new InvalidOperationException("좌우 패널의 카툰 HUD 프레임이 올바르지 않습니다.");
            }

            IsekaiTruck.Config.GameConfig config = AssetDatabase.LoadAssetAtPath<IsekaiTruck.Config.GameConfig>(
                "Assets/IsekaiTruck/Config/GameConfig.asset"
            );
            Rect wideViewport = CameraController.CalculateViewportRect(16f / 9f, config.Camera.ViewportAspect);
            uiController.SetViewport(wideViewport);
            if (leftPanel.anchorMin != Vector2.zero || leftPanel.anchorMax != new Vector2(wideViewport.xMin, 1f) ||
                gameArea.anchorMin != wideViewport.min || gameArea.anchorMax != wideViewport.max ||
                rightPanel.anchorMin != new Vector2(wideViewport.xMax, 0f) || rightPanel.anchorMax != Vector2.one)
            {
                throw new InvalidOperationException("사이드 패널과 중앙 게임 영역이 카메라 Viewport에 맞춰지지 않았습니다.");
            }

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
                Image expFill = (Image)serializedUI.FindProperty("expFill").objectReferenceValue;
                TMP_FontAsset cartoonFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CartoonFontAssetPath);

                if (levelText.font != cartoonFont || openButton.GetComponent<CartoonButtonPressEffect>() == null)
                {
                    throw new InvalidOperationException("카툰 HUD 폰트 또는 버튼 눌림 효과가 연결되지 않았습니다.");
                }

                player.AddRewards(player.RequiredExp / 2);
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

                if (speedText.text == "0 km/h")
                {
                    throw new InvalidOperationException("현재 트럭 속도가 UI에 갱신되지 않았습니다.");
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

        private static bool HasCartoonPanelStyle(RectTransform panel)
        {
            if (panel == null || panel.Find("Panel Face") == null || panel.Find("Panel Depth") == null ||
                panel.Find("Top Shine") == null || panel.GetComponent<Outline>() == null)
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

        private static TMP_FontAsset GetOrCreateCartoonFontAsset()
        {
            EnsureTmpSettings();

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(CartoonFontSourceAssetPath);
            if (sourceFont == null)
            {
                throw new InvalidOperationException($"카툰 HUD 원본 폰트를 찾지 못했습니다: {CartoonFontSourceAssetPath}");
            }

            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CartoonFontAssetPath);
            if (fontAsset != null && fontAsset.sourceFontFile == sourceFont &&
                fontAsset.atlasPadding == 9 && fontAsset.atlasTexture.filterMode == FilterMode.Bilinear)
            {
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
            if (AssetDatabase.LoadAssetAtPath<TMP_Settings>(TmpSettingsPath) != null)
            {
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
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (TMP_Settings.instance == null || Shader.Find("TextMeshPro/Mobile/Bitmap") == null)
            {
                throw new InvalidOperationException("TMP 비트맵 리소스 생성에 실패했습니다.");
            }
        }

        private static GameUIController CreateUI(Transform canvas, TMP_FontAsset font)
        {
            Color outlineColor = new Color(0.25f, 0.12f, 0.25f, 1f);
            Color sidePanelColor = new Color(0.96f, 0.89f, 0.74f, 1f);
            Color levelColor = new Color(0.47f, 0.85f, 0.70f, 1f);
            Color levelDepth = new Color(0.20f, 0.61f, 0.50f, 1f);
            Color expColor = new Color(0.76f, 0.63f, 0.91f, 1f);
            Color expDepth = new Color(0.52f, 0.38f, 0.73f, 1f);
            Color upgradeColor = new Color(1f, 0.67f, 0.39f, 1f);
            Color upgradeDepth = new Color(0.85f, 0.39f, 0.20f, 1f);
            Color goddessColor = new Color(0.91f, 0.74f, 0.93f, 1f);
            Color goddessDepth = new Color(0.68f, 0.47f, 0.73f, 1f);
            Color soulColor = new Color(0.96f, 0.67f, 0.84f, 1f);
            Color soulDepth = new Color(0.74f, 0.40f, 0.65f, 1f);
            Color speedColor = new Color(0.56f, 0.82f, 0.95f, 1f);
            Color speedDepth = new Color(0.27f, 0.59f, 0.80f, 1f);
            Color creamColor = new Color(1f, 0.97f, 0.84f, 1f);
            Color yellowColor = new Color(1f, 0.83f, 0.24f, 1f);
            Color silhouetteColor = new Color(0.25f, 0.18f, 0.29f, 1f);

            GameObject uiObject = CreateUIObject("Game UI", canvas);
            Stretch(uiObject.GetComponent<RectTransform>());
            GameUIController controller = uiObject.AddComponent<GameUIController>();

            GameObject leftPanelObject = CreateCartoonPanel("LeftPanel", uiObject.transform, sidePanelColor, new Color(0.83f, 0.72f, 0.57f, 1f), outlineColor);
            RectTransform leftPanel = leftPanelObject.GetComponent<RectTransform>();
            SetRect(leftPanel, Vector2.zero, new Vector2(0.32f, 1f), Vector2.zero, Vector2.zero);
            CreateCartoonDecorations(leftPanel);

            GameObject levelSection = CreateCartoonPanel("LevelCard", leftPanel, levelColor, levelDepth, outlineColor);
            SetRect(levelSection.GetComponent<RectTransform>(), new Vector2(0.08f, 0.73f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);
            GameObject levelBadge = CreateCirclePanel("Level Badge", levelSection.transform, creamColor);
            SetRect(levelBadge.GetComponent<RectTransform>(), new Vector2(0.31f, 0.56f), new Vector2(0.69f, 0.91f), Vector2.zero, Vector2.zero);
            CreateSparkle(levelSection.transform, new Vector2(0.82f, 0.78f), yellowColor, 18f);
            TMP_Text levelLabel = CreateText("Level Label", levelSection.transform, font, "레벨", 25, TextAlignmentOptions.Center);
            SetRect(levelLabel.rectTransform, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
            levelLabel.color = outlineColor;
            TMP_Text levelText = CreateText("Level Text", levelSection.transform, font, "Lv. 1", 44, TextAlignmentOptions.Center);
            SetRect(levelText.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);
            levelText.color = Color.white;
            AddTextOutline(levelText, outlineColor, 2f);

            GameObject expSection = CreateCartoonPanel("ExpCard", leftPanel, expColor, expDepth, outlineColor);
            SetRect(expSection.GetComponent<RectTransform>(), new Vector2(0.08f, 0.54f), new Vector2(0.92f, 0.71f), Vector2.zero, Vector2.zero);
            TMP_Text expLabel = CreateText("EXP Label", expSection.transform, font, "EXP", 21, TextAlignmentOptions.MidlineLeft);
            SetRect(expLabel.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
            expLabel.color = outlineColor;
            TMP_Text expText = CreateText("EXP Text", expSection.transform, font, "EXP 0 / 100", 19, TextAlignmentOptions.MidlineLeft);
            SetRect(expText.rectTransform, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
            expText.color = outlineColor;

            GameObject expBar = CreateCapsule("EXP Bar", expSection.transform, new Color(0.38f, 0.25f, 0.52f, 1f), outlineColor);
            SetRect(expBar.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.26f), Vector2.zero, Vector2.zero);
            GameObject expFillObject = CreatePanel("EXP Fill", expBar.transform, yellowColor);
            StretchWithOffsets(expFillObject.GetComponent<RectTransform>(), 7f, 7f, 7f, 7f);
            Image expFill = expFillObject.GetComponent<Image>();
            expFill.sprite = GetCircleSprite();
            expFill.type = Image.Type.Simple;
            expFill.rectTransform.anchorMax = new Vector2(0f, 1f);
            GameObject expShine = CreatePanel("EXP Shine", expFillObject.transform, new Color(1f, 1f, 0.72f, 0.50f));
            SetRect(expShine.GetComponent<RectTransform>(), new Vector2(0.05f, 0.58f), new Vector2(0.95f, 0.86f), Vector2.zero, Vector2.zero);
            expShine.GetComponent<Image>().raycastTarget = false;

            GameObject fuelReservedArea = CreateUIObject("Fuel Reserved Area", leftPanel);
            SetRect(fuelReservedArea.GetComponent<RectTransform>(), new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.52f), Vector2.zero, Vector2.zero);

            GameObject upgradeSection = CreateCartoonPanel("UpgradeCard", leftPanel, upgradeColor, upgradeDepth, outlineColor);
            SetRect(upgradeSection.GetComponent<RectTransform>(), new Vector2(0.08f, 0.07f), new Vector2(0.92f, 0.29f), Vector2.zero, Vector2.zero);
            TMP_Text pointText = CreateText("Point Text", upgradeSection.transform, font, "포인트 0", 22, TextAlignmentOptions.Center);
            SetRect(pointText.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.88f), Vector2.zero, Vector2.zero);
            pointText.color = outlineColor;
            Button openButton = CreateButton("Open Upgrade Button", upgradeSection.transform, font, "업그레이드", 22);
            SetRect(openButton.GetComponent<RectTransform>(), new Vector2(0.12f, 0.14f), new Vector2(0.88f, 0.52f), Vector2.zero, Vector2.zero);

            GameObject gameAreaObject = CreateUIObject("Game Area UI", uiObject.transform);
            RectTransform gameArea = gameAreaObject.GetComponent<RectTransform>();
            Stretch(gameArea);

            CreateCenterFrame(gameArea, outlineColor, creamColor);

            GameObject upgradePanel = CreatePanel("Upgrade Panel", gameArea, new Color(0.25f, 0.15f, 0.28f, 0.58f));
            Stretch(upgradePanel.GetComponent<RectTransform>());

            GameObject upgradeBox = CreateCartoonPanel("Upgrade Box", upgradePanel.transform, new Color(0.94f, 0.83f, 0.65f, 0.99f), new Color(0.76f, 0.60f, 0.43f, 1f), outlineColor);
            RectTransform upgradeBoxRect = upgradeBox.GetComponent<RectTransform>();
            upgradeBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.pivot = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.sizeDelta = new Vector2(340f, 430f);
            upgradeBoxRect.anchoredPosition = Vector2.zero;

            TMP_Text title = CreateText("Title", upgradeBox.transform, font, "트럭 업그레이드", 30, TextAlignmentOptions.Center);
            SetTopRect(title.rectTransform, 18f, 48f, 16f);
            title.color = new Color(1f, 0.56f, 0.20f, 1f);
            AddTextOutline(title, outlineColor, 2f);

            TMP_Text upgradePointText = CreateText("Upgrade Point Text", upgradeBox.transform, font, "남은 포인트: 0", 22, TextAlignmentOptions.Center);
            SetTopRect(upgradePointText.rectTransform, 68f, 34f, 20f);
            upgradePointText.color = outlineColor;

            Button speedButton = CreateButton("Speed Upgrade Button", upgradeBox.transform, font, "속도 업그레이드", 24);
            SetTopRect(speedButton.GetComponent<RectTransform>(), 115f, 58f, 22f);
            TMP_Text speedLevelText = CreateText("Speed Level", speedButton.transform, font, "Lv.0", 20, TextAlignmentOptions.MidlineRight);
            StretchWithOffsets(speedLevelText.rectTransform, 12f, 12f, 0f, 0f);
            speedLevelText.raycastTarget = false;

            TMP_Text speedStatText = CreateText("Speed Stat", upgradeBox.transform, font, "최대 속도: 0.100", 19, TextAlignmentOptions.Center);
            SetTopRect(speedStatText.rectTransform, 178f, 30f, 22f);
            speedStatText.color = outlineColor;

            Button sizeButton = CreateButton("Size Upgrade Button", upgradeBox.transform, font, "크기 업그레이드", 24);
            SetTopRect(sizeButton.GetComponent<RectTransform>(), 225f, 58f, 22f);
            TMP_Text sizeLevelText = CreateText("Size Level", sizeButton.transform, font, "Lv.0", 20, TextAlignmentOptions.MidlineRight);
            StretchWithOffsets(sizeLevelText.rectTransform, 12f, 12f, 0f, 0f);
            sizeLevelText.raycastTarget = false;

            TMP_Text sizeStatText = CreateText("Size Stat", upgradeBox.transform, font, "트럭 크기: 100%", 19, TextAlignmentOptions.Center);
            SetTopRect(sizeStatText.rectTransform, 288f, 30f, 22f);
            sizeStatText.color = outlineColor;

            Button closeButton = CreateButton("Close Button", upgradeBox.transform, font, "닫기", 22);
            SetTopRect(closeButton.GetComponent<RectTransform>(), 342f, 54f, 22f);

            GameObject rightPanelObject = CreateCartoonPanel("RightPanel", uiObject.transform, sidePanelColor, new Color(0.83f, 0.72f, 0.57f, 1f), outlineColor);
            RectTransform rightPanel = rightPanelObject.GetComponent<RectTransform>();
            SetRect(rightPanel, new Vector2(0.68f, 0f), Vector2.one, Vector2.zero, Vector2.zero);
            CreateCartoonDecorations(rightPanel);

            GameObject goddessArea = CreateCartoonPanel("GoddessCard", rightPanel, goddessColor, goddessDepth, outlineColor);
            SetRect(goddessArea.GetComponent<RectTransform>(), new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.94f), Vector2.zero, Vector2.zero);

            GameObject portraitFrame = CreateCirclePanel("Portrait Frame", goddessArea.transform, creamColor);
            SetRect(portraitFrame.GetComponent<RectTransform>(), new Vector2(0.20f, 0.31f), new Vector2(0.80f, 0.91f), Vector2.zero, Vector2.zero);
            Outline portraitOutline = portraitFrame.AddComponent<Outline>();
            portraitOutline.effectColor = outlineColor;
            portraitOutline.effectDistance = new Vector2(3f, -3f);
            GameObject portraitBackground = CreateCirclePanel("Portrait Background", portraitFrame.transform, new Color(0.68f, 0.88f, 0.94f, 1f));
            StretchWithOffsets(portraitBackground.GetComponent<RectTransform>(), 9f, 9f, 9f, 9f);
            CreateSparkle(goddessArea.transform, new Vector2(0.82f, 0.80f), yellowColor, 20f);
            CreateSparkle(goddessArea.transform, new Vector2(0.18f, 0.66f), Color.white, 13f);

            GameObject silhouette = CreateUIObject("Goddess Silhouette", goddessArea.transform);
            SetRect(silhouette.GetComponent<RectTransform>(), new Vector2(0.27f, 0.32f), new Vector2(0.73f, 0.84f), Vector2.zero, Vector2.zero);
            GameObject head = CreateCirclePanel("Head", silhouette.transform, silhouetteColor);
            SetRect(head.GetComponent<RectTransform>(), new Vector2(0.36f, 0.72f), new Vector2(0.64f, 0.94f), Vector2.zero, Vector2.zero);
            GameObject body = CreatePanel("Body", silhouette.transform, silhouetteColor);
            SetRect(body.GetComponent<RectTransform>(), new Vector2(0.32f, 0.25f), new Vector2(0.68f, 0.73f), Vector2.zero, Vector2.zero);
            GameObject leftArm = CreatePanel("Left Arm", silhouette.transform, silhouetteColor);
            SetRect(leftArm.GetComponent<RectTransform>(), new Vector2(0.18f, 0.30f), new Vector2(0.34f, 0.70f), Vector2.zero, Vector2.zero);
            leftArm.transform.localRotation = Quaternion.Euler(0f, 0f, -12f);
            GameObject rightArm = CreatePanel("Right Arm", silhouette.transform, silhouetteColor);
            SetRect(rightArm.GetComponent<RectTransform>(), new Vector2(0.66f, 0.30f), new Vector2(0.82f, 0.70f), Vector2.zero, Vector2.zero);
            rightArm.transform.localRotation = Quaternion.Euler(0f, 0f, 12f);

            GameObject speechBubble = CreateSpeechBubble("Speech Bubble", goddessArea.transform, creamColor, outlineColor);
            SetRect(speechBubble.GetComponent<RectTransform>(), new Vector2(0.08f, 0.05f), new Vector2(0.92f, 0.26f), Vector2.zero, Vector2.zero);
            TMP_Text goddessMessage = CreateText("Goddess Message", speechBubble.transform, font, "여신이 지켜보고 있습니다", 18, TextAlignmentOptions.Center);
            StretchWithOffsets(goddessMessage.rectTransform, 14f, 14f, 7f, 7f);
            goddessMessage.color = outlineColor;

            GameObject soulSection = CreateCartoonPanel("SoulCard", rightPanel, soulColor, soulDepth, outlineColor);
            SetRect(soulSection.GetComponent<RectTransform>(), new Vector2(0.08f, 0.31f), new Vector2(0.92f, 0.47f), Vector2.zero, Vector2.zero);
            CreateSoulIcon(soulSection.transform, new Vector2(0.17f, 0.69f), creamColor, outlineColor);
            TMP_Text soulLabel = CreateText("Soul Label", soulSection.transform, font, "영혼", 21, TextAlignmentOptions.Center);
            SetRect(soulLabel.rectTransform, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
            soulLabel.color = outlineColor;
            TMP_Text soulText = CreateText("Soul Text", soulSection.transform, font, "0", 36, TextAlignmentOptions.Center);
            SetRect(soulText.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);
            soulText.color = Color.white;
            AddTextOutline(soulText, outlineColor, 2f);

            GameObject speedSection = CreateCartoonPanel("SpeedCard", rightPanel, speedColor, speedDepth, outlineColor);
            SetRect(speedSection.GetComponent<RectTransform>(), new Vector2(0.08f, 0.12f), new Vector2(0.92f, 0.28f), Vector2.zero, Vector2.zero);
            CreateSpeedIcon(speedSection.transform, new Vector2(0.18f, 0.72f), creamColor);
            TMP_Text speedLabel = CreateText("Speed Label", speedSection.transform, font, "속도", 21, TextAlignmentOptions.Center);
            SetRect(speedLabel.rectTransform, new Vector2(0.08f, 0.56f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
            speedLabel.color = outlineColor;
            TMP_Text speedText = CreateText("Speed Text", speedSection.transform, font, "0 km/h", 34, TextAlignmentOptions.Center);
            SetRect(speedText.rectTransform, new Vector2(0.08f, 0.10f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);
            speedText.color = Color.white;
            AddTextOutline(speedText, outlineColor, 2f);

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
                speedStatText,
                sizeStatText,
                openButton,
                closeButton,
                speedButton,
                sizeButton
            );

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

        private static GameObject CreateCirclePanel(string name, Transform parent, Color color)
        {
            GameObject panel = CreatePanel(name, parent, color);
            Image image = panel.GetComponent<Image>();
            image.sprite = GetCircleSprite();
            image.type = Image.Type.Simple;
            image.raycastTarget = false;
            return panel;
        }

        private static GameObject CreateCartoonPanel(string name, Transform parent, Color faceColor, Color depthColor, Color outlineColor)
        {
            GameObject panel = CreatePanel(name, parent, outlineColor);

            Outline outline = panel.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject depth = CreatePanel("Panel Depth", panel.transform, depthColor);
            StretchWithOffsets(depth.GetComponent<RectTransform>(), 6f, 6f, 6f, 6f);
            depth.GetComponent<Image>().raycastTarget = false;

            GameObject face = CreatePanel("Panel Face", panel.transform, faceColor);
            StretchWithOffsets(face.GetComponent<RectTransform>(), 7f, 7f, 14f, 7f);
            face.GetComponent<Image>().raycastTarget = false;

            GameObject topShine = CreatePanel("Top Shine", panel.transform, new Color(1f, 1f, 1f, 0.34f));
            SetRect(topShine.GetComponent<RectTransform>(), new Vector2(0.10f, 0.82f), new Vector2(0.90f, 0.91f), Vector2.zero, Vector2.zero);
            topShine.GetComponent<Image>().raycastTarget = false;
            return panel;
        }

        private static GameObject CreateCapsule(string name, Transform parent, Color faceColor, Color outlineColor)
        {
            GameObject capsule = CreatePanel(name, parent, outlineColor);
            Image capsuleImage = capsule.GetComponent<Image>();
            capsuleImage.sprite = GetCircleSprite();
            capsuleImage.type = Image.Type.Simple;
            Outline outline = capsule.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject face = CreatePanel("Capsule Face", capsule.transform, faceColor);
            Image faceImage = face.GetComponent<Image>();
            faceImage.sprite = GetCircleSprite();
            faceImage.type = Image.Type.Simple;
            StretchWithOffsets(face.GetComponent<RectTransform>(), 5f, 5f, 5f, 5f);
            faceImage.raycastTarget = false;
            return capsule;
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
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

            GameObject tail = CreatePanel("Bubble Tail", bubble.transform, faceColor);
            SetRect(tail.GetComponent<RectTransform>(), new Vector2(0.16f, 0f), new Vector2(0.16f, 0f), new Vector2(-7f, -7f), new Vector2(7f, 7f));
            tail.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            Outline tailOutline = tail.AddComponent<Outline>();
            tailOutline.effectColor = outlineColor;
            tailOutline.effectDistance = new Vector2(2f, -2f);
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
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);

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
            outline.effectColor = color;
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
            text.fontStyle = FontStyles.Bold;
            text.characterSpacing = 0f;
            text.enableAutoSizing = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, TMP_FontAsset font, string label, int fontSize)
        {
            Color outlineColor = new Color(0.34f, 0.17f, 0.16f, 1f);
            GameObject buttonObject = CreatePanel(name, parent, outlineColor);
            Outline outline = buttonObject.AddComponent<Outline>();
            outline.effectColor = outlineColor;
            outline.effectDistance = new Vector2(2f, -2f);
            Button button = buttonObject.AddComponent<Button>();

            GameObject depth = CreatePanel("Button Depth", buttonObject.transform, new Color(0.82f, 0.35f, 0.14f, 1f));
            StretchWithOffsets(depth.GetComponent<RectTransform>(), 5f, 5f, 5f, 5f);
            depth.GetComponent<Image>().raycastTarget = false;

            GameObject face = CreatePanel("Button Face", buttonObject.transform, new Color(1f, 0.66f, 0.20f, 1f));
            StretchWithOffsets(face.GetComponent<RectTransform>(), 6f, 6f, 11f, 6f);
            Image faceImage = face.GetComponent<Image>();
            button.targetGraphic = faceImage;

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 0.88f, 0.58f, 1f);
            colors.pressedColor = new Color(0.94f, 0.74f, 0.56f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.75f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.08f;
            button.colors = colors;

            GameObject highlight = CreatePanel("Button Highlight", face.transform, new Color(1f, 0.96f, 0.66f, 0.72f));
            SetRect(highlight.GetComponent<RectTransform>(), new Vector2(0.12f, 0.70f), new Vector2(0.88f, 0.86f), Vector2.zero, Vector2.zero);
            highlight.GetComponent<Image>().raycastTarget = false;

            TMP_Text text = CreateText("Label", face.transform, font, label, fontSize, TextAlignmentOptions.Center);
            text.color = Color.white;
            StretchWithOffsets(text.rectTransform, 10f, 10f, 0f, 0f);
            AddTextOutline(text, outlineColor, 2f);

            CartoonButtonPressEffect pressEffect = buttonObject.AddComponent<CartoonButtonPressEffect>();
            pressEffect.SetTarget(face.GetComponent<RectTransform>());
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
