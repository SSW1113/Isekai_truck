using System;
using System.Collections.Generic;
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
using IsekaiTruck.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class WorldTravelFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string BlessingCatalogPath = "Assets/IsekaiTruck/Blessings/BlessingCatalog.asset";
        private const string WorldFolder = "Assets/IsekaiTruck/Worlds";
        private const string DefinitionFolder = WorldFolder + "/Definitions";
        private const string CatalogPath = WorldFolder + "/WorldCatalog.asset";
        private const string OriginalWorldPath = DefinitionFolder + "/OriginalWorld.asset";
        private const string DarkWorldPath = DefinitionFolder + "/DarkWorld.asset";
        private const string VerificationSaveKey = "IsekaiTruck.WorldTravelVerification";

        [MenuItem("Isekai Truck/Setup World Travel Feature")]
        public static void Setup()
        {
            EnsureFolder(WorldFolder);
            EnsureFolder(DefinitionFolder);

            WorldDefinition originalWorld = GetOrCreateWorld(
                OriginalWorldPath,
                "original_world",
                "초원의 세계",
                new Color32(0x87, 0xce, 0xeb, 0xff),
                new Color32(0x87, 0xce, 0xeb, 0xff),
                new Color32(0x3a, 0x7a, 0x2a, 0xff),
                new Color32(0x2f, 0x66, 0x22, 0xff));
            WorldDefinition darkWorld = GetOrCreateWorld(
                DarkWorldPath,
                "dark_world",
                "어두운 세계",
                new Color32(0x1b, 0x24, 0x32, 0xff),
                new Color32(0x25, 0x31, 0x42, 0xff),
                new Color32(0x25, 0x34, 0x2a, 0xff),
                new Color32(0x19, 0x25, 0x1d, 0xff));
            WorldCatalog catalog = GetOrCreateCatalog(originalWorld, darkWorld);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (gameManager == null || canvas == null)
            {
                throw new InvalidOperationException("Main scene GameManager or Canvas was not found.");
            }

            WorldTravelSystem travelSystem = Object.FindFirstObjectByType<WorldTravelSystem>();
            if (travelSystem == null)
            {
                GameObject systemObject = new GameObject("World Travel System");
                travelSystem = systemObject.AddComponent<WorldTravelSystem>();
            }
            travelSystem.SetCatalog(catalog);

            Transform existingUI = canvas.transform.Find("World Travel UI");
            if (existingUI != null)
            {
                Object.DestroyImmediate(existingUI.gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            WorldTravelUIController travelUI = CreateUI(canvas.transform, font);
            MoveBehindModalUIs(travelUI.transform, canvas.transform);
            gameManager.SetWorldTravelSystems(travelSystem, travelUI);

            EditorUtility.SetDirty(travelSystem);
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "세계 이동 시스템과 두 개의 세계를 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            WorldCatalog catalog = AssetDatabase.LoadAssetAtPath<WorldCatalog>(CatalogPath);
            BlessingCatalog blessingCatalog = AssetDatabase.LoadAssetAtPath<BlessingCatalog>(BlessingCatalogPath);
            WorldDefinition originalWorld = AssetDatabase.LoadAssetAtPath<WorldDefinition>(OriginalWorldPath);
            WorldDefinition darkWorld = AssetDatabase.LoadAssetAtPath<WorldDefinition>(DarkWorldPath);
            if (config == null || catalog == null || blessingCatalog == null || originalWorld == null || darkWorld == null)
            {
                throw new InvalidOperationException("World travel assets are missing.");
            }

            if (config.Wanted.WorldTravelUnlockLevel != 5 || catalog.Worlds.Count < 2)
            {
                throw new InvalidOperationException("World travel configuration is incorrect.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            WorldTravelSystem travelSystem = Object.FindFirstObjectByType<WorldTravelSystem>();
            WorldTravelUIController travelUI = Object.FindFirstObjectByType<WorldTravelUIController>();
            if (gameManager == null || travelSystem == null || travelUI == null)
            {
                throw new InvalidOperationException("World travel scene systems are missing.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            if (serializedGameManager.FindProperty("worldTravelSystem").objectReferenceValue != travelSystem || serializedGameManager.FindProperty("worldTravelUIController").objectReferenceValue != travelUI)
            {
                throw new InvalidOperationException("GameManager world travel references are missing.");
            }

            SerializedObject serializedTravelSystem = new SerializedObject(travelSystem);
            if (serializedTravelSystem.FindProperty("worldCatalog").objectReferenceValue != catalog)
            {
                throw new InvalidOperationException("World catalog reference is missing.");
            }

            SerializedObject serializedUI = new SerializedObject(travelUI);
            if (serializedUI.FindProperty("gameArea").objectReferenceValue == null ||
                serializedUI.FindProperty("currentWorldText").objectReferenceValue == null ||
                serializedUI.FindProperty("openButton").objectReferenceValue == null ||
                serializedUI.FindProperty("confirmationPopup").objectReferenceValue == null)
            {
                throw new InvalidOperationException("World travel UI references are incomplete.");
            }

            VerifyTravelLogic(config, catalog);
            VerifyWorldAppearance(config, originalWorld, darkWorld);
            VerifySave(config, blessingCatalog, catalog);
            Debug.Log("World travel feature verification passed.");
        }

        private static void VerifyTravelLogic(GameConfig config, WorldCatalog catalog)
        {
            GameObject root = new GameObject("World Travel Logic Verification");
            try
            {
                WantedLevelSystem wanted = root.AddComponent<WantedLevelSystem>();
                WorldTravelSystem travel = root.AddComponent<WorldTravelSystem>();
                wanted.Initialize(config);
                travel.SetCatalog(catalog);
                travel.Initialize(config, wanted);

                int unlockKills = config.Wanted.KillsPerLevel * config.Wanted.WorldTravelUnlockLevel;
                wanted.RestoreState(unlockKills - 1);
                if (travel.CanTravel || travel.TryTravel(out _))
                {
                    throw new InvalidOperationException("World travel unlocked before wanted level 5.");
                }

                wanted.RestoreState(unlockKills);
                WorldDefinition previousWorld = travel.CurrentWorld;
                if (!travel.CanTravel || !travel.TryTravel(out WorldTravelResult result))
                {
                    throw new InvalidOperationException("World travel did not unlock at wanted level 5.");
                }

                if (result.PreviousWorld != previousWorld || result.DestinationWorld == previousWorld || wanted.TotalKills != 0 || wanted.Level != 0)
                {
                    throw new InvalidOperationException("World travel did not change worlds and reset wanted progress correctly.");
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void VerifyWorldAppearance(GameConfig config, WorldDefinition originalWorld, WorldDefinition darkWorld)
        {
            GameObject root = new GameObject("World Appearance Verification");
            GameObject player = new GameObject("World Appearance Player");
            GameObject cameraObject = new GameObject("World Appearance Camera", typeof(UnityEngine.Camera));
            try
            {
                UnityEngine.Camera targetCamera = cameraObject.GetComponent<UnityEngine.Camera>();
                WorldManager worldManager = root.AddComponent<WorldManager>();
                worldManager.Initialize(config, player.transform, targetCamera, originalWorld);
                worldManager.ApplyWorld(darkWorld);

                MeshRenderer tileRenderer = root.GetComponentInChildren<MeshRenderer>();
                Texture2D groundTexture = tileRenderer != null ? tileRenderer.sharedMaterial.mainTexture as Texture2D : null;
                if (worldManager.CurrentWorld != darkWorld || RenderSettings.fogColor != darkWorld.FogColor || targetCamera.backgroundColor != darkWorld.SkyColor || groundTexture == null)
                {
                    throw new InvalidOperationException("Dark world appearance was not applied.");
                }

                if (groundTexture.GetPixel(0, 0) != darkWorld.GroundColor || groundTexture.GetPixel(1, 0) != darkWorld.GroundPatternColor)
                {
                    throw new InvalidOperationException("Dark world ground colors were not applied.");
                }
            }
            finally
            {
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(player);
                Object.DestroyImmediate(root);
            }
        }

        private static void VerifySave(GameConfig config, BlessingCatalog blessingCatalog, WorldCatalog worldCatalog)
        {
            PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            GameObject source = new GameObject("World Travel Save Source");
            GameObject restored = new GameObject("World Travel Save Restored");
            try
            {
                SaveSystems first = CreateSaveSystems(source, config, blessingCatalog, worldCatalog);
                int unlockKills = config.Wanted.KillsPerLevel * config.Wanted.WorldTravelUnlockLevel;
                first.Wanted.RestoreState(unlockKills);
                if (!first.Travel.TryTravel(out WorldTravelResult result))
                {
                    throw new InvalidOperationException("World travel save verification could not travel.");
                }

                string destinationId = result.DestinationWorld.Id;
                first.Wanted.RestoreState(75);
                first.Save.Save();
                Object.DestroyImmediate(first.Save);

                SaveSystems second = CreateSaveSystems(restored, config, blessingCatalog, worldCatalog);
                if (second.Travel.CurrentWorld.Id != destinationId || second.Wanted.TotalKills != 75)
                {
                    throw new InvalidOperationException("World and wanted progress were not restored from save data.");
                }
            }
            finally
            {
                Object.DestroyImmediate(restored);
                Object.DestroyImmediate(source);
                PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            }
        }

        private static SaveSystems CreateSaveSystems(GameObject root, GameConfig config, BlessingCatalog blessingCatalog, WorldCatalog worldCatalog)
        {
            TruckController truck = root.AddComponent<TruckController>();
            PlayerState player = root.AddComponent<PlayerState>();
            BlessingSystem blessings = root.AddComponent<BlessingSystem>();
            BlessingLoadoutSystem loadout = root.AddComponent<BlessingLoadoutSystem>();
            RebirthSystem rebirth = root.AddComponent<RebirthSystem>();
            WantedLevelSystem wanted = root.AddComponent<WantedLevelSystem>();
            WorldTravelSystem travel = root.AddComponent<WorldTravelSystem>();
            TruckUpgradeSystem upgrades = root.AddComponent<TruckUpgradeSystem>();
            PlayerProgressSaveSystem save = root.AddComponent<PlayerProgressSaveSystem>();

            truck.Initialize(config);
            player.Initialize(config);
            blessings.SetCatalog(blessingCatalog);
            blessings.Initialize();
            loadout.Initialize(config, blessings);
            rebirth.Initialize(config, player, truck, blessings);
            wanted.Initialize(config);
            travel.SetCatalog(worldCatalog);
            travel.Initialize(config, wanted);
            upgrades.Initialize(player, truck);
            save.SetSaveKeyForVerification(VerificationSaveKey);
            save.Initialize(player, truck, rebirth, blessings, loadout, wanted, null, travel, upgrades);
            return new SaveSystems(wanted, travel, save);
        }

        private static WorldDefinition GetOrCreateWorld(
            string path,
            string id,
            string displayName,
            Color skyColor,
            Color fogColor,
            Color groundColor,
            Color groundPatternColor
        )
        {
            WorldDefinition definition = AssetDatabase.LoadAssetAtPath<WorldDefinition>(path);
            if (definition != null)
            {
                return definition;
            }

            definition = ScriptableObject.CreateInstance<WorldDefinition>();
            definition.SetEditorValues(id, displayName, skyColor, fogColor, groundColor, groundPatternColor);
            AssetDatabase.CreateAsset(definition, path);
            EditorUtility.SetDirty(definition);
            return definition;
        }

        private static WorldCatalog GetOrCreateCatalog(WorldDefinition originalWorld, WorldDefinition darkWorld)
        {
            WorldCatalog catalog = AssetDatabase.LoadAssetAtPath<WorldCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WorldCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            List<WorldDefinition> worlds = new List<WorldDefinition> { originalWorld, darkWorld };
            for (int i = 0; i < catalog.Worlds.Count; i++)
            {
                WorldDefinition existingWorld = catalog.Worlds[i];
                if (existingWorld != null && existingWorld.Id != originalWorld.Id && existingWorld.Id != darkWorld.Id)
                {
                    worlds.Add(existingWorld);
                }
            }

            catalog.SetWorlds(worlds);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static WorldTravelUIController CreateUI(Transform canvas, Font font)
        {
            GameObject uiObject = CreateUIObject("World Travel UI", canvas);
            Stretch(uiObject.GetComponent<RectTransform>());
            WorldTravelUIController controller = uiObject.AddComponent<WorldTravelUIController>();

            GameObject gameAreaObject = CreateUIObject("World Travel Game Area", uiObject.transform);
            RectTransform gameArea = gameAreaObject.GetComponent<RectTransform>();
            Stretch(gameArea);

            GameObject statusPanel = CreatePanel("Current World Panel", gameArea, new Color(0f, 0f, 0f, 0.55f));
            RectTransform statusRect = statusPanel.GetComponent<RectTransform>();
            statusRect.anchorMin = new Vector2(0f, 1f);
            statusRect.anchorMax = new Vector2(0f, 1f);
            statusRect.pivot = new Vector2(0f, 1f);
            statusRect.anchoredPosition = new Vector2(14f, -236f);
            statusRect.sizeDelta = new Vector2(390f, 54f);
            statusPanel.GetComponent<Image>().raycastTarget = false;

            Text currentWorldText = CreateText("Current World Text", statusPanel.transform, font, "현재 세계: 초원의 세계", 20, TextAnchor.MiddleCenter);
            StretchWithOffsets(currentWorldText.rectTransform, 10f, 10f, 4f, 4f);

            Button openButton = CreateButton("Open World Travel Button", gameArea, font, "세계 이동 (지명수배 Lv.5)", 18);
            SetRect(openButton.GetComponent<RectTransform>(), new Vector2(0.63f, 1f), Vector2.one, new Vector2(0f, -290f), new Vector2(-14f, -232f));
            Text openButtonText = openButton.GetComponentInChildren<Text>();

            GameObject popup = CreatePanel("World Travel Confirmation Popup", gameArea, new Color(0f, 0f, 0f, 0.78f));
            Stretch(popup.GetComponent<RectTransform>());

            GameObject box = CreatePanel("World Travel Confirmation Box", popup.transform, new Color(0.08f, 0.08f, 0.1f, 1f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(540f, 360f);

            Text title = CreateText("World Travel Title", box.transform, font, "세계 이동", 31, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, 22f, 52f, 24f);
            Text confirmationText = CreateText("World Travel Confirmation Text", box.transform, font, string.Empty, 21, TextAnchor.MiddleCenter);
            confirmationText.horizontalOverflow = HorizontalWrapMode.Wrap;
            confirmationText.verticalOverflow = VerticalWrapMode.Overflow;
            SetTopRect(confirmationText.rectTransform, 88f, 140f, 34f);

            Button confirmButton = CreateButton("Confirm World Travel Button", box.transform, font, "이동하기", 23);
            SetRect(confirmButton.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(28f, 28f), new Vector2(-8f, 92f));
            Button cancelButton = CreateButton("Cancel World Travel Button", box.transform, font, "취소", 23);
            SetRect(cancelButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(8f, 28f), new Vector2(-28f, 92f));

            controller.SetReferences(gameArea, currentWorldText, openButton, openButtonText, popup, confirmationText, confirmButton, cancelButton);
            popup.SetActive(false);
            return controller;
        }

        private static void MoveBehindModalUIs(Transform ui, Transform canvas)
        {
            int siblingIndex = canvas.childCount - 1;
            Transform gameUI = canvas.Find("Game UI");
            Transform rebirthUI = canvas.Find("Rebirth UI");
            Transform blessingUI = canvas.Find("Blessing Inventory UI");
            if (gameUI != null) siblingIndex = Mathf.Min(siblingIndex, gameUI.GetSiblingIndex());
            if (rebirthUI != null) siblingIndex = Mathf.Min(siblingIndex, rebirthUI.GetSiblingIndex());
            if (blessingUI != null) siblingIndex = Mathf.Min(siblingIndex, blessingUI.GetSiblingIndex());
            ui.SetSiblingIndex(siblingIndex);
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separator = path.LastIndexOf('/');
            string parent = path.Substring(0, separator);
            string name = path.Substring(separator + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
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
            StretchWithOffsets(text.rectTransform, 8f, 8f, 4f, 4f);
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
            SetRect(rectTransform, new Vector2(0f, 1f), Vector2.one, new Vector2(horizontalMargin, -top - height), new Vector2(-horizontalMargin, -top));
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
            public SaveSystems(WantedLevelSystem wanted, WorldTravelSystem travel, PlayerProgressSaveSystem save)
            {
                Wanted = wanted;
                Travel = travel;
                Save = save;
            }

            public WantedLevelSystem Wanted { get; }
            public WorldTravelSystem Travel { get; }
            public PlayerProgressSaveSystem Save { get; }
        }
    }
}
