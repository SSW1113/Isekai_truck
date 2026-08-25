using System;
using IsekaiTruck.Blessings;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Player;
using IsekaiTruck.Rebirth;
using IsekaiTruck.Save;
using IsekaiTruck.Truck;
using IsekaiTruck.UI;
using IsekaiTruck.Upgrades;
using IsekaiTruck.Wanted;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class WantedLevelFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string CatalogPath = "Assets/IsekaiTruck/Blessings/BlessingCatalog.asset";
        private const string CartoonFontPath = "Assets/IsekaiTruck/Fonts/CartoonHUD.asset";
        private const string WantedFontCharacters = "비상지명수배단계LV.0123456789!★";
        private const string VerificationSaveKey = "IsekaiTruck.WantedLevelVerification";
        private const int MaxStarCount = 10;
        private const float StarSize = 27f;
        private const float StageGap = 8f;

        [MenuItem("Isekai Truck/Setup Wanted Level Feature")]
        public static void Setup()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            GameUIController gameUI = Object.FindFirstObjectByType<GameUIController>();
            if (config == null || gameManager == null || gameUI == null)
            {
                throw new InvalidOperationException("Wanted level setup dependencies were not found.");
            }

            WantedLevelSystem wantedLevelSystem = Object.FindFirstObjectByType<WantedLevelSystem>();
            if (wantedLevelSystem == null)
            {
                GameObject systemObject = new GameObject("Wanted Level System");
                wantedLevelSystem = systemObject.AddComponent<WantedLevelSystem>();
            }

            SerializedObject serializedGameUI = new SerializedObject(gameUI);
            RectTransform gameArea = (RectTransform)serializedGameUI.FindProperty("gameArea").objectReferenceValue;
            GameObject upgradePanel = (GameObject)serializedGameUI.FindProperty("upgradePanel").objectReferenceValue;
            if (gameArea == null)
            {
                throw new InvalidOperationException("Game UI viewport reference is missing.");
            }

            Transform existingUI = gameArea.Find("Wanted Level UI") ?? gameArea.Find("Game Area UI/Wanted Level UI");
            if (existingUI != null)
            {
                Object.DestroyImmediate(existingUI.gameObject);
            }

            TMP_FontAsset cartoonFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CartoonFontPath);
            if (cartoonFont == null)
            {
                throw new InvalidOperationException("Cartoon HUD font was not found.");
            }

            EnsureWantedFontCharacters(cartoonFont);
            WantedLevelUIController wantedLevelUI = CreateUI(gameArea, cartoonFont);
            if (upgradePanel != null && upgradePanel.transform.parent == gameArea)
            {
                wantedLevelUI.transform.SetSiblingIndex(upgradePanel.transform.GetSiblingIndex());
            }

            gameManager.SetWantedLevelSystems(wantedLevelSystem, wantedLevelUI);
            MainHudLayoutSetup.ApplyToLoadedScene();
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "지명수배 레벨과 HUD를 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            BlessingCatalog catalog = AssetDatabase.LoadAssetAtPath<BlessingCatalog>(CatalogPath);
            if (config == null || catalog == null || config.Wanted.KillsPerLevel != 50 || config.Wanted.MaxLevel != 10)
            {
                throw new InvalidOperationException("Wanted level configuration is incorrect.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            WantedLevelSystem wantedLevelSystem = Object.FindFirstObjectByType<WantedLevelSystem>();
            WantedLevelUIController wantedLevelUI = Object.FindFirstObjectByType<WantedLevelUIController>(FindObjectsInactive.Include);
            if (gameManager == null || wantedLevelSystem == null || wantedLevelUI == null)
            {
                throw new InvalidOperationException("Wanted level scene systems are missing.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            if (serializedGameManager.FindProperty("wantedLevelSystem").objectReferenceValue != wantedLevelSystem || serializedGameManager.FindProperty("wantedLevelUIController").objectReferenceValue != wantedLevelUI)
            {
                throw new InvalidOperationException("GameManager wanted level references are missing.");
            }

            SerializedObject serializedUI = new SerializedObject(wantedLevelUI);
            SerializedProperty statusText = serializedUI.FindProperty("statusText");
            SerializedProperty levelText = serializedUI.FindProperty("levelText");
            SerializedProperty bannerFace = serializedUI.FindProperty("bannerFace");
            WantedLevelUIPresentation presentation = (WantedLevelUIPresentation)serializedUI.FindProperty("presentation").objectReferenceValue;
            RectTransform wantedRect = wantedLevelUI.GetComponent<RectTransform>();
            SerializedObject serializedPresentation = presentation != null ? new SerializedObject(presentation) : null;
            if (statusText.objectReferenceValue == null ||
                levelText.objectReferenceValue == null ||
                bannerFace.objectReferenceValue == null ||
                presentation == null ||
                serializedPresentation.FindProperty("starIcons").arraySize != MaxStarCount ||
                serializedPresentation.FindProperty("starCanvasGroups").arraySize != MaxStarCount ||
                serializedPresentation.FindProperty("stageText").objectReferenceValue == null ||
                serializedPresentation.FindProperty("redBeacon").objectReferenceValue == null ||
                serializedPresentation.FindProperty("blueBeacon").objectReferenceValue == null ||
                !wantedLevelUI.gameObject.activeSelf ||
                wantedRect.anchorMin != new Vector2(0.5f, 1f) ||
                wantedRect.anchorMax != new Vector2(0.5f, 1f))
            {
                throw new InvalidOperationException("Wanted level UI references are incomplete.");
            }

            VerifyLevelProgression(config);
            VerifyUI(config);
            VerifyRebirthRetention(config, catalog);
            VerifySave(config, catalog);
            Debug.Log("Wanted level feature verification passed.");
        }

        private static void VerifyLevelProgression(GameConfig config)
        {
            GameObject systemObject = new GameObject("Wanted Level Verification");

            try
            {
                WantedLevelSystem wanted = systemObject.AddComponent<WantedLevelSystem>();
                wanted.Initialize(config);

                for (int i = 0; i < 49; i++) wanted.RegisterKill();
                if (wanted.TotalKills != 49 || wanted.Level != 0)
                {
                    throw new InvalidOperationException("Wanted level increased before the first threshold.");
                }

                wanted.RegisterKill();
                if (wanted.TotalKills != 50 || wanted.Level != 1)
                {
                    throw new InvalidOperationException("Wanted level did not increase at 50 kills.");
                }

                wanted.RestoreState(499);
                if (wanted.Level != 9)
                {
                    throw new InvalidOperationException("Wanted level 9 boundary is incorrect.");
                }

                wanted.RestoreState(500);
                wanted.RegisterKill();
                if (wanted.TotalKills != 501 || wanted.Level != 10)
                {
                    throw new InvalidOperationException("Wanted level did not remain capped at 10.");
                }
            }
            finally
            {
                Object.DestroyImmediate(systemObject);
            }
        }

        private static void VerifyUI(GameConfig config)
        {
            GameObject systemObject = new GameObject("Wanted UI Verification System");
            GameObject uiRoot = new GameObject("Wanted UI Verification Root", typeof(RectTransform));

            try
            {
                WantedLevelSystem wanted = systemObject.AddComponent<WantedLevelSystem>();
                wanted.Initialize(config);
                TMP_FontAsset cartoonFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(CartoonFontPath);
                WantedLevelUIController ui = CreateUI(uiRoot.GetComponent<RectTransform>(), cartoonFont);
                ui.Initialize(wanted);

                SerializedObject serializedUI = new SerializedObject(ui);
                TMP_Text statusText = (TMP_Text)serializedUI.FindProperty("statusText").objectReferenceValue;
                TMP_Text levelText = (TMP_Text)serializedUI.FindProperty("levelText").objectReferenceValue;
                Image bannerFace = (Image)serializedUI.FindProperty("bannerFace").objectReferenceValue;
                WantedLevelUIPresentation presentation = (WantedLevelUIPresentation)serializedUI.FindProperty("presentation").objectReferenceValue;

                if (ui.gameObject.activeSelf || presentation.VisibleStarCount != 0)
                {
                    throw new InvalidOperationException("Wanted level 0 UI was not hidden.");
                }

                wanted.RestoreState(50);
                if (statusText.text != "비상! 지명수배" ||
                    levelText.text != "LV.1" ||
                    !HudColorPalette.Matches(bannerFace.color, HudColorPalette.Wanted) ||
                    !ui.gameObject.activeSelf ||
                    !presentation.IsAssemblyPlaying ||
                    presentation.VisibleStarCount != 1)
                {
                    throw new InvalidOperationException("Wanted level 1 assembly state is incorrect.");
                }

                presentation.CompleteAnimationsImmediately();
                wanted.RestoreState(250);
                presentation.CompleteAnimationsImmediately();
                if (presentation.VisibleStarCount != 5 || !presentation.IsContinuousBeaconActive)
                {
                    throw new InvalidOperationException("Wanted level 5 continuous alert state is incorrect.");
                }

                wanted.RestoreState(500);
                presentation.CompleteAnimationsImmediately();
                if (levelText.text != "LV.10" || presentation.VisibleStarCount != MaxStarCount)
                {
                    throw new InvalidOperationException("Wanted level 10 star count is incorrect.");
                }

                for (int level = 1; level <= MaxStarCount; level++)
                {
                    presentation.ShowInitialState(level);
                    if (presentation.VisibleStarCount != level)
                    {
                        throw new InvalidOperationException($"Wanted level {level} star count is incorrect.");
                    }

                    VerifyStageLabelGap(presentation);
                }

                wanted.ResetForWorldTravel();
                if (ui.gameObject.activeSelf)
                {
                    throw new InvalidOperationException("Wanted UI remained visible after a world travel reset.");
                }

                GameObject restoredSystemObject = new GameObject("Wanted Restored UI Verification System");
                restoredSystemObject.transform.SetParent(uiRoot.transform, false);
                WantedLevelSystem restoredWanted = restoredSystemObject.AddComponent<WantedLevelSystem>();
                restoredWanted.Initialize(config);
                restoredWanted.RestoreState(250);
                WantedLevelUIController restoredUI = CreateUI(uiRoot.GetComponent<RectTransform>(), cartoonFont);
                restoredUI.Initialize(restoredWanted);
                SerializedObject serializedRestoredUI = new SerializedObject(restoredUI);
                WantedLevelUIPresentation restoredPresentation = (WantedLevelUIPresentation)serializedRestoredUI.FindProperty("presentation").objectReferenceValue;
                if (!restoredUI.gameObject.activeSelf || restoredPresentation.IsAssemblyPlaying || !restoredPresentation.IsContinuousBeaconActive)
                {
                    throw new InvalidOperationException("Restored wanted UI did not appear in its completed state.");
                }
            }
            finally
            {
                Object.DestroyImmediate(uiRoot);
                Object.DestroyImmediate(systemObject);
            }
        }

        private static void VerifyStageLabelGap(WantedLevelUIPresentation presentation)
        {
            SerializedObject serializedPresentation = new SerializedObject(presentation);
            SerializedProperty stars = serializedPresentation.FindProperty("starIcons");
            RectTransform stageText = (RectTransform)serializedPresentation.FindProperty("stageText").objectReferenceValue;
            float rightmostEdge = float.MinValue;

            for (int i = 0; i < stars.arraySize; i++)
            {
                RectTransform star = (RectTransform)stars.GetArrayElementAtIndex(i).objectReferenceValue;
                if (star.gameObject.activeSelf)
                {
                    rightmostEdge = Mathf.Max(rightmostEdge, star.anchoredPosition.x + star.sizeDelta.x * 0.5f);
                }
            }

            float labelLeftEdge = stageText.anchoredPosition.x - stageText.sizeDelta.x * 0.5f;
            if (!Mathf.Approximately(labelLeftEdge - rightmostEdge, StageGap))
            {
                throw new InvalidOperationException("Wanted stage label spacing is inconsistent.");
            }
        }

        private static void VerifySave(GameConfig config, BlessingCatalog catalog)
        {
            PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            GameObject source = new GameObject("Wanted Save Source");
            GameObject restored = new GameObject("Wanted Save Restored");

            try
            {
                SaveSystems first = CreateSaveSystems(source, config, catalog);
                first.Wanted.RestoreState(175);
                first.Save.Save();
                Object.DestroyImmediate(first.Save);

                SaveSystems second = CreateSaveSystems(restored, config, catalog);
                if (second.Wanted.TotalKills != 175 || second.Wanted.Level != 3)
                {
                    throw new InvalidOperationException("Wanted level progress was not restored from save data.");
                }
            }
            finally
            {
                Object.DestroyImmediate(restored);
                Object.DestroyImmediate(source);
                PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            }
        }

        private static void VerifyRebirthRetention(GameConfig config, BlessingCatalog catalog)
        {
            GameObject root = new GameObject("Wanted Rebirth Verification");

            try
            {
                TruckController truck = root.AddComponent<TruckController>();
                PlayerState player = root.AddComponent<PlayerState>();
                BlessingSystem blessings = root.AddComponent<BlessingSystem>();
                RebirthSystem rebirth = root.AddComponent<RebirthSystem>();
                WantedLevelSystem wanted = root.AddComponent<WantedLevelSystem>();

                truck.Initialize(config);
                player.Initialize(config);
                blessings.SetCatalog(catalog);
                blessings.Initialize();
                rebirth.Initialize(config, player, truck, blessings);
                wanted.Initialize(config);

                player.RestoreState(10, 0, 0, 0, 0f, 0f);
                wanted.RestoreState(175);
                if (!rebirth.BeginRebirth(0) || !rebirth.CompleteRebirth(0, out RebirthResult result) || wanted.TotalKills != 175 || wanted.Level != 3)
                {
                    throw new InvalidOperationException("Wanted level progress did not survive rebirth.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static SaveSystems CreateSaveSystems(GameObject root, GameConfig config, BlessingCatalog catalog)
        {
            TruckController truck = root.AddComponent<TruckController>();
            PlayerState player = root.AddComponent<PlayerState>();
            BlessingSystem blessings = root.AddComponent<BlessingSystem>();
            BlessingLoadoutSystem loadout = root.AddComponent<BlessingLoadoutSystem>();
            RebirthSystem rebirth = root.AddComponent<RebirthSystem>();
            WantedLevelSystem wanted = root.AddComponent<WantedLevelSystem>();
            TruckUpgradeSystem upgrades = root.AddComponent<TruckUpgradeSystem>();
            PlayerProgressSaveSystem save = root.AddComponent<PlayerProgressSaveSystem>();

            truck.Initialize(config);
            player.Initialize(config);
            blessings.SetCatalog(catalog);
            blessings.Initialize();
            loadout.Initialize(config, blessings);
            rebirth.Initialize(config, player, truck, blessings);
            wanted.Initialize(config);
            upgrades.Initialize(player, truck);
            save.SetSaveKeyForVerification(VerificationSaveKey);
            save.Initialize(player, truck, rebirth, blessings, loadout, wanted, upgrades);
            return new SaveSystems(wanted, save);
        }

        private static WantedLevelUIController CreateUI(Transform parent, TMP_FontAsset cartoonFont)
        {
            GameObject panel = CreateUIObject("Wanted Level UI", parent);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 1f);
            panelRect.anchorMax = new Vector2(0.5f, 1f);
            panelRect.pivot = new Vector2(0.5f, 1f);
            panelRect.anchoredPosition = new Vector2(0f, -22f);
            panelRect.sizeDelta = new Vector2(470f, 110f);

            GameObject animationRootObject = CreateUIObject("Animation Root", panel.transform);
            RectTransform animationRoot = animationRootObject.GetComponent<RectTransform>();
            Stretch(animationRoot);

            GameObject leftTail = CreatePanel("Left Alert Tail", animationRoot, HudColorPalette.WantedDepth);
            SetFixedRect(leftTail.GetComponent<RectTransform>(), new Vector2(-218f, -49f), new Vector2(66f, 44f));
            leftTail.transform.localRotation = Quaternion.Euler(0f, 0f, -8f);

            GameObject rightTail = CreatePanel("Right Alert Tail", animationRoot, HudColorPalette.WantedDepth);
            SetFixedRect(rightTail.GetComponent<RectTransform>(), new Vector2(218f, -49f), new Vector2(66f, 44f));
            rightTail.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);

            GameObject shadow = CreatePanel("Banner Shadow", animationRoot, new Color(
                HudColorPalette.WantedDepth.r,
                HudColorPalette.WantedDepth.g,
                HudColorPalette.WantedDepth.b,
                0.72f
            ));
            SetFixedRect(shadow.GetComponent<RectTransform>(), new Vector2(0f, -53f), new Vector2(424f, 66f));

            GameObject face = CreatePanel("Banner Face", animationRoot, HudColorPalette.Wanted);
            RectTransform faceRect = face.GetComponent<RectTransform>();
            SetFixedRect(faceRect, new Vector2(0f, -47f), new Vector2(412f, 62f));
            AddPanelDepth(face, HudColorPalette.WantedDepth);
            Image faceImage = face.GetComponent<Image>();

            GameObject faceHighlight = CreatePanel("Banner Highlight", face.transform, new Color(1f, 1f, 1f, 0.14f));
            SetFixedRect(faceHighlight.GetComponent<RectTransform>(), new Vector2(0f, 19f), new Vector2(370f, 8f));

            GameObject beaconHousing = CreatePanel("Beacon Housing", animationRoot, HudColorPalette.WantedTrack);
            SetFixedRect(beaconHousing.GetComponent<RectTransform>(), new Vector2(0f, -11f), new Vector2(112f, 22f));
            AddPanelDepth(beaconHousing, HudColorPalette.WantedDepth);

            GameObject redBeaconObject = CreatePanel("Red Beacon", animationRoot, HudColorPalette.WantedBeaconRed);
            SetFixedRect(redBeaconObject.GetComponent<RectTransform>(), new Vector2(-27f, -10f), new Vector2(48f, 18f));
            CanvasGroup redBeacon = redBeaconObject.AddComponent<CanvasGroup>();
            redBeacon.alpha = 0.28f;

            GameObject blueBeaconObject = CreatePanel("Blue Beacon", animationRoot, HudColorPalette.WantedBeaconBlue);
            SetFixedRect(blueBeaconObject.GetComponent<RectTransform>(), new Vector2(27f, -10f), new Vector2(48f, 18f));
            CanvasGroup blueBeacon = blueBeaconObject.AddComponent<CanvasGroup>();
            blueBeacon.alpha = 0.24f;

            GameObject contentObject = CreateUIObject("Banner Content", animationRoot);
            RectTransform contentRect = contentObject.GetComponent<RectTransform>();
            Stretch(contentRect);
            CanvasGroup contentGroup = contentObject.AddComponent<CanvasGroup>();

            TMP_Text statusText = CreateTmpText(
                "Wanted Status Text",
                contentObject.transform,
                cartoonFont,
                "비상! 지명수배",
                24,
                TextAlignmentOptions.MidlineLeft
            );
            SetFixedRect(statusText.rectTransform, new Vector2(-54f, -47f), new Vector2(270f, 44f));
            statusText.color = HudColorPalette.WantedStar;
            AddTextOutline(statusText, HudColorPalette.WantedDepth, 1.2f);

            TMP_Text levelText = CreateTmpText(
                "Wanted Level Text",
                contentObject.transform,
                cartoonFont,
                "LV.0",
                31,
                TextAlignmentOptions.Center
            );
            SetFixedRect(levelText.rectTransform, new Vector2(145f, -47f), new Vector2(110f, 44f));
            levelText.color = new Color32(0xFF, 0xFB, 0xF2, 0xFF);
            AddTextOutline(levelText, HudColorPalette.WantedDepth, 1.2f);

            GameObject stageTrack = CreatePanel("Wanted Stage Track", contentObject.transform, HudColorPalette.WantedTrack);
            SetFixedRect(stageTrack.GetComponent<RectTransform>(), new Vector2(0f, -86f), new Vector2(350f, 30f));
            AddPanelDepth(stageTrack, HudColorPalette.WantedDepth);

            TMP_Text stageLabel = CreateTmpText(
                "Wanted Stage Label",
                stageTrack.transform,
                cartoonFont,
                "단계",
                17,
                TextAlignmentOptions.Center
            );
            SetFixedRect(stageLabel.rectTransform, Vector2.zero, new Vector2(48f, 26f));
            stageLabel.color = new Color32(0xF4, 0xE7, 0xC3, 0xFF);
            AddTextOutline(stageLabel, HudColorPalette.WantedDepth, 0.8f);
            stageLabel.gameObject.SetActive(false);

            RectTransform[] starIcons = new RectTransform[MaxStarCount];
            CanvasGroup[] starCanvasGroups = new CanvasGroup[MaxStarCount];
            for (int i = 0; i < starIcons.Length; i++)
            {
                TMP_Text star = CreateTmpText(
                    $"Wanted Star {i + 1}",
                    stageTrack.transform,
                    cartoonFont,
                    "★",
                    27,
                    TextAlignmentOptions.Center
                );
                SetFixedRect(star.rectTransform, Vector2.zero, new Vector2(StarSize, StarSize));
                star.color = HudColorPalette.WantedStar;
                AddTextOutline(star, HudColorPalette.WantedDepth, 0.7f);
                CanvasGroup starCanvasGroup = star.gameObject.AddComponent<CanvasGroup>();
                starIcons[i] = star.rectTransform;
                starCanvasGroups[i] = starCanvasGroup;
                star.gameObject.SetActive(false);
            }

            WantedLevelUIPresentation presentation = panel.AddComponent<WantedLevelUIPresentation>();
            presentation.SetReferences(
                parent as RectTransform,
                animationRoot,
                new[] { leftTail.GetComponent<RectTransform>(), redBeaconObject.GetComponent<RectTransform>() },
                new[] { rightTail.GetComponent<RectTransform>(), blueBeaconObject.GetComponent<RectTransform>() },
                new[] { shadow.GetComponent<RectTransform>(), faceRect, beaconHousing.GetComponent<RectTransform>() },
                contentGroup,
                starIcons,
                starCanvasGroups,
                stageLabel.rectTransform,
                redBeacon,
                blueBeacon
            );

            WantedLevelUIController controller = panel.AddComponent<WantedLevelUIController>();
            controller.SetReferences(
                statusText,
                levelText,
                faceImage,
                presentation
            );
            panel.SetActive(true);
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
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = color;
            image.raycastTarget = false;
            return panel;
        }

        private static TMP_Text CreateTmpText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string value,
            int fontSize,
            TextAlignmentOptions alignment
        )
        {
            GameObject textObject = CreateUIObject(name, parent);
            TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = value;
            text.fontSize = fontSize;
            text.alignment = alignment;
            text.fontStyle = FontStyles.Normal;
            text.enableAutoSizing = false;
            text.raycastTarget = false;
            return text;
        }

        private static void SetFixedRect(RectTransform rectTransform, Vector2 position, Vector2 size)
        {
            rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
            rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            rectTransform.pivot = new Vector2(0.5f, 0.5f);
            rectTransform.anchoredPosition = position;
            rectTransform.sizeDelta = size;
        }

        private static void AddPanelDepth(GameObject target, Color depthColor)
        {
            Outline outline = target.AddComponent<Outline>();
            outline.effectColor = new Color(depthColor.r, depthColor.g, depthColor.b, 0.42f);
            outline.effectDistance = new Vector2(1f, -1f);

            Shadow shadow = target.AddComponent<Shadow>();
            shadow.effectColor = new Color(depthColor.r, depthColor.g, depthColor.b, 0.34f);
            shadow.effectDistance = new Vector2(0f, -4f);
        }

        private static void AddTextOutline(TMP_Text text, Color color, float distance)
        {
            Outline outline = text.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(color.r, color.g, color.b, 0.82f);
            outline.effectDistance = new Vector2(distance, -distance);
        }

        private static void EnsureWantedFontCharacters(TMP_FontAsset fontAsset)
        {
            AtlasPopulationMode populationMode = fontAsset.atlasPopulationMode;
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
            if (!fontAsset.TryAddCharacters(WantedFontCharacters, out string missingCharacters))
            {
                Debug.LogWarning($"지명수배 HUD 폰트에서 생성하지 못한 문자가 있습니다: {missingCharacters}");
            }

            fontAsset.atlasPopulationMode = populationMode;
            EditorUtility.SetDirty(fontAsset);
            if (fontAsset.atlasTexture != null)
            {
                EditorUtility.SetDirty(fontAsset.atlasTexture);
            }
        }

        private static void Stretch(RectTransform rectTransform)
        {
            SetRect(rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private sealed class SaveSystems
        {
            public SaveSystems(WantedLevelSystem wanted, PlayerProgressSaveSystem save)
            {
                Wanted = wanted;
                Save = save;
            }

            public WantedLevelSystem Wanted { get; }
            public PlayerProgressSaveSystem Save { get; }
        }
    }
}
