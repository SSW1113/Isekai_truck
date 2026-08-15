using System;
using IsekaiTruck.Camera;
using IsekaiTruck.Core;
using IsekaiTruck.Input;
using IsekaiTruck.Player;
using IsekaiTruck.Truck;
using IsekaiTruck.UI;
using IsekaiTruck.Upgrades;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class SeventhStageSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";

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

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
                "gameArea", "upgradePanel", "levelText", "expText", "expFill", "soulText", "pointText",
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

            GameObject truckObject = new GameObject("UI Verification Truck");
            GameObject playerObject = new GameObject("UI Verification Player");
            GameObject upgradeObject = new GameObject("UI Verification Upgrade System");

            try
            {
                TruckController truck = truckObject.AddComponent<TruckController>();
                PlayerState player = playerObject.AddComponent<PlayerState>();
                TruckUpgradeSystem upgrades = upgradeObject.AddComponent<TruckUpgradeSystem>();
                IsekaiTruck.Config.GameConfig config = AssetDatabase.LoadAssetAtPath<IsekaiTruck.Config.GameConfig>(
                    "Assets/IsekaiTruck/Config/GameConfig.asset"
                );

                truck.Initialize(config);
                player.Initialize(config);
                upgrades.Initialize(player, truck);
                uiController.Initialize(player, truck, upgrades, joystickInput, cameraController);

                Button openButton = (Button)serializedUI.FindProperty("openButton").objectReferenceValue;
                Button closeButton = (Button)serializedUI.FindProperty("closeButton").objectReferenceValue;
                Button speedButton = (Button)serializedUI.FindProperty("speedButton").objectReferenceValue;
                Text levelText = (Text)serializedUI.FindProperty("levelText").objectReferenceValue;
                Text pointText = (Text)serializedUI.FindProperty("pointText").objectReferenceValue;
                Image expFill = (Image)serializedUI.FindProperty("expFill").objectReferenceValue;

                int requiredExp = player.RequiredExp;
                int halfExp = requiredExp / 2;
                player.AddRewards(halfExp);
                float expectedExpRatio = (float)halfExp / requiredExp;
                if (!Mathf.Approximately(expFill.rectTransform.anchorMax.x, expectedExpRatio))
                {
                    throw new InvalidOperationException("EXP bar does not match the current EXP ratio.");
                }

                player.AddRewards(requiredExp - halfExp);
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

                if (levelText.text != "Lv. 2" || pointText.text != "포인트 0")
                {
                    throw new InvalidOperationException("플레이어 HUD 텍스트가 갱신되지 않았습니다.");
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

        private static GameUIController CreateUI(Transform canvas, Font font)
        {
            GameObject uiObject = CreateUIObject("Game UI", canvas);
            Stretch(uiObject.GetComponent<RectTransform>());
            GameUIController controller = uiObject.AddComponent<GameUIController>();

            GameObject gameAreaObject = CreateUIObject("Game Area UI", uiObject.transform);
            RectTransform gameArea = gameAreaObject.GetComponent<RectTransform>();
            Stretch(gameArea);

            GameObject hudObject = CreatePanel("Player HUD", gameArea, new Color(0f, 0f, 0f, 0.55f));
            SetRect(hudObject.GetComponent<RectTransform>(), new Vector2(0f, 1f), Vector2.one, new Vector2(14f, -94f), new Vector2(-14f, -14f));

            Text levelText = CreateText("Level Text", hudObject.transform, font, "Lv. 1", 27, TextAnchor.MiddleCenter);
            SetRect(levelText.rectTransform, new Vector2(0f, 0.52f), new Vector2(0.18f, 1f), new Vector2(8f, 0f), Vector2.zero);

            Text soulText = CreateText("Soul Text", hudObject.transform, font, "영혼 0", 21, TextAnchor.MiddleCenter);
            SetRect(soulText.rectTransform, new Vector2(0.18f, 0.52f), new Vector2(0.43f, 1f), Vector2.zero, Vector2.zero);

            Text pointText = CreateText("Point Text", hudObject.transform, font, "포인트 0", 21, TextAnchor.MiddleCenter);
            SetRect(pointText.rectTransform, new Vector2(0.43f, 0.52f), new Vector2(0.70f, 1f), Vector2.zero, Vector2.zero);

            Button openButton = CreateButton("Open Upgrade Button", hudObject.transform, font, "업그레이드", 20);
            SetRect(openButton.GetComponent<RectTransform>(), new Vector2(0.72f, 0.59f), new Vector2(0.98f, 0.94f), Vector2.zero, Vector2.zero);

            Text expText = CreateText("EXP Text", hudObject.transform, font, "EXP 0 / 100", 18, TextAnchor.MiddleLeft);
            SetRect(expText.rectTransform, new Vector2(0f, 0.27f), new Vector2(1f, 0.53f), new Vector2(12f, 0f), new Vector2(-12f, 0f));

            GameObject expBar = CreatePanel("EXP Bar", hudObject.transform, new Color(1f, 1f, 1f, 0.25f));
            SetRect(expBar.GetComponent<RectTransform>(), new Vector2(0f, 0.10f), new Vector2(1f, 0.25f), new Vector2(12f, 0f), new Vector2(-12f, 0f));

            GameObject expFillObject = CreatePanel("EXP Fill", expBar.transform, new Color(1f, 0.83f, 0.23f, 1f));
            RectTransform expFillRect = expFillObject.GetComponent<RectTransform>();
            Stretch(expFillRect);
            expFillRect.anchorMax = new Vector2(0f, 1f);
            Image expFill = expFillObject.GetComponent<Image>();
            expFill.type = Image.Type.Simple;

            GameObject upgradePanel = CreatePanel("Upgrade Panel", gameArea, new Color(0f, 0f, 0f, 0.62f));
            Stretch(upgradePanel.GetComponent<RectTransform>());

            GameObject upgradeBox = CreatePanel("Upgrade Box", upgradePanel.transform, new Color(0.08f, 0.08f, 0.08f, 0.97f));
            RectTransform upgradeBoxRect = upgradeBox.GetComponent<RectTransform>();
            upgradeBoxRect.anchorMin = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.anchorMax = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.pivot = new Vector2(0.5f, 0.5f);
            upgradeBoxRect.sizeDelta = new Vector2(340f, 430f);
            upgradeBoxRect.anchoredPosition = Vector2.zero;

            Text title = CreateText("Title", upgradeBox.transform, font, "트럭 업그레이드", 30, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, 18f, 48f, 16f);

            Text upgradePointText = CreateText("Upgrade Point Text", upgradeBox.transform, font, "남은 포인트: 0", 22, TextAnchor.MiddleCenter);
            SetTopRect(upgradePointText.rectTransform, 68f, 34f, 20f);

            Button speedButton = CreateButton("Speed Upgrade Button", upgradeBox.transform, font, "속도 업그레이드", 24);
            SetTopRect(speedButton.GetComponent<RectTransform>(), 115f, 58f, 22f);
            Text speedLevelText = CreateText("Speed Level", speedButton.transform, font, "Lv.0", 20, TextAnchor.MiddleRight);
            StretchWithOffsets(speedLevelText.rectTransform, 12f, 12f, 0f, 0f);
            speedLevelText.raycastTarget = false;

            Text speedStatText = CreateText("Speed Stat", upgradeBox.transform, font, "최대 속도: 0.100", 19, TextAnchor.MiddleCenter);
            SetTopRect(speedStatText.rectTransform, 178f, 30f, 22f);

            Button sizeButton = CreateButton("Size Upgrade Button", upgradeBox.transform, font, "크기 업그레이드", 24);
            SetTopRect(sizeButton.GetComponent<RectTransform>(), 225f, 58f, 22f);
            Text sizeLevelText = CreateText("Size Level", sizeButton.transform, font, "Lv.0", 20, TextAnchor.MiddleRight);
            StretchWithOffsets(sizeLevelText.rectTransform, 12f, 12f, 0f, 0f);
            sizeLevelText.raycastTarget = false;

            Text sizeStatText = CreateText("Size Stat", upgradeBox.transform, font, "트럭 크기: 100%", 19, TextAnchor.MiddleCenter);
            SetTopRect(sizeStatText.rectTransform, 288f, 30f, 22f);

            Button closeButton = CreateButton("Close Button", upgradeBox.transform, font, "닫기", 22);
            SetTopRect(closeButton.GetComponent<RectTransform>(), 342f, 54f, 22f);

            controller.SetReferences(
                gameArea,
                upgradePanel,
                levelText,
                expText,
                expFill,
                soulText,
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
            return panel;
        }

        private static Text CreateText(string name, Transform parent, Font font, string value, int fontSize, TextAnchor alignment)
        {
            GameObject textObject = CreateUIObject(name, parent);
            Text text = textObject.AddComponent<Text>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.color = Color.white;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(string name, Transform parent, Font font, string label, int fontSize)
        {
            GameObject buttonObject = CreatePanel(name, parent, new Color(0.88f, 0.88f, 0.88f, 1f));
            Button button = buttonObject.AddComponent<Button>();
            button.targetGraphic = buttonObject.GetComponent<Image>();

            Text text = CreateText("Label", buttonObject.transform, font, label, fontSize, TextAnchor.MiddleCenter);
            text.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            StretchWithOffsets(text.rectTransform, 10f, 10f, 0f, 0f);
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
