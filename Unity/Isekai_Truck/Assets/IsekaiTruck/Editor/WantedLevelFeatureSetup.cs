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
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class WantedLevelFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string CatalogPath = "Assets/IsekaiTruck/Blessings/BlessingCatalog.asset";
        private const string VerificationSaveKey = "IsekaiTruck.WantedLevelVerification";
        private const float StarWidth = 42f;

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
            if (gameArea == null)
            {
                throw new InvalidOperationException("Game UI viewport reference is missing.");
            }

            Transform existingUI = gameArea.Find("Wanted Level UI");
            if (existingUI != null)
            {
                Object.DestroyImmediate(existingUI.gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            WantedLevelUIController wantedLevelUI = CreateUI(gameArea, font);
            gameManager.SetWantedLevelSystems(wantedLevelSystem, wantedLevelUI);
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
            WantedLevelUIController wantedLevelUI = Object.FindFirstObjectByType<WantedLevelUIController>();
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
            SerializedProperty levelText = serializedUI.FindProperty("levelText");
            SerializedProperty starFillMasks = serializedUI.FindProperty("starFillMasks");
            if (levelText.objectReferenceValue == null || starFillMasks.arraySize != 5)
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
                Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                WantedLevelUIController ui = CreateUI(uiRoot.GetComponent<RectTransform>(), font);
                ui.Initialize(wanted);

                SerializedObject serializedUI = new SerializedObject(ui);
                Text levelText = (Text)serializedUI.FindProperty("levelText").objectReferenceValue;
                SerializedProperty masks = serializedUI.FindProperty("starFillMasks");
                RectTransform firstMask = (RectTransform)masks.GetArrayElementAtIndex(0).objectReferenceValue;
                RectTransform fifthMask = (RectTransform)masks.GetArrayElementAtIndex(4).objectReferenceValue;

                wanted.RestoreState(50);
                if (levelText.text != "지명수배 Lv.1" || !Mathf.Approximately(firstMask.sizeDelta.x, StarWidth * 0.5f))
                {
                    throw new InvalidOperationException("Wanted level 1 half-star display is incorrect.");
                }

                wanted.RestoreState(500);
                if (levelText.text != "지명수배 Lv.10" || !Mathf.Approximately(firstMask.sizeDelta.x, StarWidth) || !Mathf.Approximately(fifthMask.sizeDelta.x, StarWidth))
                {
                    throw new InvalidOperationException("Wanted level 10 star display is incorrect.");
                }
            }
            finally
            {
                Object.DestroyImmediate(uiRoot);
                Object.DestroyImmediate(systemObject);
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

        private static WantedLevelUIController CreateUI(Transform parent, Font font)
        {
            GameObject panel = CreatePanel("Wanted Level UI", parent, new Color(0f, 0f, 0f, 0.55f));
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(14f, -106f);
            panelRect.sizeDelta = new Vector2(390f, 66f);
            panel.GetComponent<Image>().raycastTarget = false;

            WantedLevelUIController controller = panel.AddComponent<WantedLevelUIController>();
            Text levelText = CreateText("Wanted Level Text", panel.transform, font, "지명수배 Lv.0", 21, TextAnchor.MiddleCenter);
            SetRect(levelText.rectTransform, Vector2.zero, new Vector2(0f, 1f), Vector2.zero, new Vector2(145f, 0f));

            RectTransform[] starFillMasks = new RectTransform[5];
            for (int i = 0; i < starFillMasks.Length; i++)
            {
                GameObject slot = CreateUIObject($"Wanted Star {i + 1}", panel.transform);
                RectTransform slotRect = slot.GetComponent<RectTransform>();
                slotRect.anchorMin = new Vector2(0f, 0.5f);
                slotRect.anchorMax = new Vector2(0f, 0.5f);
                slotRect.pivot = new Vector2(0f, 0.5f);
                slotRect.anchoredPosition = new Vector2(150f + i * 46f, 0f);
                slotRect.sizeDelta = new Vector2(StarWidth, StarWidth);

                Text background = CreateText("Empty Star", slot.transform, font, "★", 40, TextAnchor.MiddleCenter);
                Stretch(background.rectTransform);
                background.color = new Color(0.28f, 0.28f, 0.28f, 1f);

                GameObject maskObject = CreateUIObject("Star Fill Mask", slot.transform);
                RectTransform maskRect = maskObject.GetComponent<RectTransform>();
                maskRect.anchorMin = new Vector2(0f, 0.5f);
                maskRect.anchorMax = new Vector2(0f, 0.5f);
                maskRect.pivot = new Vector2(0f, 0.5f);
                maskRect.anchoredPosition = Vector2.zero;
                maskRect.sizeDelta = new Vector2(0f, StarWidth);
                maskObject.AddComponent<RectMask2D>();
                starFillMasks[i] = maskRect;

                Text fill = CreateText("Filled Star", maskObject.transform, font, "★", 40, TextAnchor.MiddleCenter);
                RectTransform fillRect = fill.rectTransform;
                fillRect.anchorMin = new Vector2(0f, 0.5f);
                fillRect.anchorMax = new Vector2(0f, 0.5f);
                fillRect.pivot = new Vector2(0f, 0.5f);
                fillRect.anchoredPosition = Vector2.zero;
                fillRect.sizeDelta = new Vector2(StarWidth, StarWidth);
                fill.color = new Color(1f, 0.82f, 0.18f, 1f);
            }

            controller.SetReferences(levelText, starFillMasks, StarWidth);
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
