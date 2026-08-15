using System;
using System.Collections.Generic;
using IsekaiTruck.Blessings;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Input;
using IsekaiTruck.Player;
using IsekaiTruck.Rebirth;
using IsekaiTruck.Save;
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
    public static class RebirthFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string BlessingFolder = "Assets/IsekaiTruck/Blessings";
        private const string DefinitionFolder = BlessingFolder + "/Definitions";
        private const string CatalogPath = BlessingFolder + "/BlessingCatalog.asset";
        private const string VerificationSaveKey = "IsekaiTruck.RebirthVerification";

        [MenuItem("Isekai Truck/Setup Rebirth Feature")]
        public static void Setup()
        {
            EnsureFolder(BlessingFolder);
            EnsureFolder(DefinitionFolder);

            BlessingCatalog catalog = CreateBlessingCatalog();
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException("GameConfig asset was not found.");
            }

            EditorUtility.SetDirty(config);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (gameManager == null || canvas == null)
            {
                throw new InvalidOperationException("Main scene GameManager or Canvas was not found.");
            }

            GameObject existingSystems = GameObject.Find("Rebirth Systems");
            if (existingSystems != null)
            {
                Object.DestroyImmediate(existingSystems);
            }

            Transform existingUI = canvas.transform.Find("Rebirth UI");
            if (existingUI != null)
            {
                Object.DestroyImmediate(existingUI.gameObject);
            }

            GameObject systemsObject = new GameObject("Rebirth Systems");
            BlessingSystem blessingSystem = systemsObject.AddComponent<BlessingSystem>();
            RebirthSystem rebirthSystem = systemsObject.AddComponent<RebirthSystem>();
            PlayerProgressSaveSystem saveSystem = systemsObject.AddComponent<PlayerProgressSaveSystem>();
            blessingSystem.SetCatalog(catalog);

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            RebirthUIController uiController = CreateUI(canvas.transform, font);
            gameManager.SetRebirthSystems(blessingSystem, rebirthSystem, saveSystem, uiController);
            EditorUtility.SetDirty(gameManager);
            EditorUtility.SetDirty(blessingSystem);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "환생, 축복 선택, 진행 저장 시스템을 Main 씬에 연결했습니다.", "확인");
            }
        }

        [MenuItem("Isekai Truck/Setup Rebirth Confirmation Popup")]
        public static void SetupConfirmationPopup()
        {
            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            RebirthUIController controller = Object.FindFirstObjectByType<RebirthUIController>();
            if (controller == null)
            {
                throw new InvalidOperationException("RebirthUIController was not found.");
            }

            SerializedObject serializedUI = new SerializedObject(controller);
            GameObject rebirthPanel = (GameObject)serializedUI.FindProperty("rebirthPanel").objectReferenceValue;
            if (rebirthPanel == null)
            {
                throw new InvalidOperationException("Rebirth Panel reference is missing.");
            }

            Transform existingPopup = rebirthPanel.transform.Find("Rebirth Confirmation Popup");
            if (existingPopup != null)
            {
                Object.DestroyImmediate(existingPopup.gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            GameObject confirmationPopup = CreateConfirmationPopup(rebirthPanel.transform, font, out Text confirmationText, out Button confirmPopupButton, out Button cancelPopupButton);
            controller.SetConfirmationReferences(confirmationPopup, confirmationText, confirmPopupButton, cancelPopupButton);
            EditorUtility.SetDirty(controller);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();
        }

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            BlessingCatalog catalog = AssetDatabase.LoadAssetAtPath<BlessingCatalog>(CatalogPath);
            VerifyConfiguration(config, catalog);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            BlessingSystem sceneBlessings = Object.FindFirstObjectByType<BlessingSystem>();
            RebirthSystem sceneRebirth = Object.FindFirstObjectByType<RebirthSystem>();
            PlayerProgressSaveSystem sceneSave = Object.FindFirstObjectByType<PlayerProgressSaveSystem>();
            RebirthUIController sceneUI = Object.FindFirstObjectByType<RebirthUIController>();
            if (gameManager == null || sceneBlessings == null || sceneRebirth == null || sceneSave == null || sceneUI == null)
            {
                throw new InvalidOperationException("Rebirth scene systems are missing.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            string[] managerReferences = { "blessingSystem", "rebirthSystem", "saveSystem", "rebirthUIController" };
            for (int i = 0; i < managerReferences.Length; i++)
            {
                if (serializedGameManager.FindProperty(managerReferences[i]).objectReferenceValue == null)
                {
                    throw new InvalidOperationException($"GameManager reference is missing: {managerReferences[i]}");
                }
            }

            SerializedObject serializedUI = new SerializedObject(sceneUI);
            string[] uiReferences =
            {
                "gameArea", "rebirthPanel", "tierPanel", "candidatePanel", "statusText", "guideText",
                "openButton", "closeButton", "confirmButton", "confirmationPopup", "confirmationText", "confirmPopupButton",
                "cancelPopupButton", "tierButtons", "tierLabels", "candidateButtons", "candidateLabels"
            };
            for (int i = 0; i < uiReferences.Length; i++)
            {
                SerializedProperty property = serializedUI.FindProperty(uiReferences[i]);
                bool isMissing = property == null || property.isArray && property.arraySize == 0 || !property.isArray && property.objectReferenceValue == null;
                if (isMissing)
                {
                    throw new InvalidOperationException($"RebirthUIController reference is missing: {uiReferences[i]}");
                }
            }

            GameObject rebirthPanel = (GameObject)serializedUI.FindProperty("rebirthPanel").objectReferenceValue;
            GameObject confirmationPopup = (GameObject)serializedUI.FindProperty("confirmationPopup").objectReferenceValue;
            if (confirmationPopup.transform.parent != rebirthPanel.transform || confirmationPopup.activeSelf)
            {
                throw new InvalidOperationException("Rebirth confirmation popup hierarchy or initial state is incorrect.");
            }

            VerifyRuntimeFlow(config, catalog);
            VerifySaveRestore(config, catalog);
            Debug.Log("Rebirth feature verification passed.");
        }

        public static void VerifyRegressions()
        {
            SecondStageSetup.Verify();
            ThirdStageSetup.Verify();
            FourthStageSetup.Verify();
            FifthStageSetup.Verify();
            SixthStageSetup.Verify();
            SeventhStageSetup.Verify();
            DeltaTimeMigrationVerification.Verify();
            MonsterPrefabSetup.Verify();
            GroundPatternVerification.Verify();
            Debug.Log("All existing feature regressions passed.");
        }

        private static void VerifyConfiguration(GameConfig config, BlessingCatalog catalog)
        {
            if (config == null || config.Rebirth.Tiers.Length != 10 || config.Rebirth.BlessingCandidateCount != 3)
            {
                throw new InvalidOperationException("Rebirth configuration is incomplete.");
            }

            for (int i = 0; i < config.Rebirth.Tiers.Length; i++)
            {
                GameConfig.RebirthTierSettings tier = config.Rebirth.Tiers[i];
                if (tier.RequiredLevel != (i + 1) * 10 || !Mathf.Approximately(tier.TotalWeight, 100f))
                {
                    throw new InvalidOperationException($"Rebirth tier configuration is invalid at index {i}.");
                }
            }

            if (!Mathf.Approximately(config.Rebirth.Tiers[0].CWeight, 90f) || !Mathf.Approximately(config.Rebirth.Tiers[0].UWeight, 9f) || !Mathf.Approximately(config.Rebirth.Tiers[0].RWeight, 1f) || !Mathf.Approximately(config.Rebirth.Tiers[0].SrWeight, 0f))
            {
                throw new InvalidOperationException("Level 10 rarity weights do not match 90/9/1/0.");
            }

            int[] gradeCounts = new int[4];
            for (int i = 0; i < catalog.Definitions.Count; i++)
            {
                BlessingDefinition definition = catalog.Definitions[i];
                if (definition == null || string.IsNullOrEmpty(definition.Id))
                {
                    throw new InvalidOperationException("Blessing catalog contains an invalid definition.");
                }

                gradeCounts[(int)definition.Grade]++;
            }

            for (int i = 0; i < gradeCounts.Length; i++)
            {
                if (gradeCounts[i] < 3)
                {
                    throw new InvalidOperationException($"Blessing grade {(BlessingGrade)i} needs at least three definitions.");
                }
            }
        }

        private static void VerifyRuntimeFlow(GameConfig config, BlessingCatalog catalog)
        {
            GameObject truckObject = new GameObject("Rebirth Verification Truck");
            GameObject playerObject = new GameObject("Rebirth Verification Player");
            GameObject blessingObject = new GameObject("Rebirth Verification Blessings");
            GameObject rebirthObject = new GameObject("Rebirth Verification System");

            try
            {
                TruckController truck = truckObject.AddComponent<TruckController>();
                PlayerState player = playerObject.AddComponent<PlayerState>();
                BlessingSystem blessings = blessingObject.AddComponent<BlessingSystem>();
                RebirthSystem rebirth = rebirthObject.AddComponent<RebirthSystem>();
                truck.Initialize(config);
                player.Initialize(config);
                blessings.SetCatalog(catalog);
                blessings.Initialize();
                rebirth.Initialize(config, player, truck, blessings);

                player.RestoreState(10, 0, 50, 3, 0f, 0f);
                truck.UpgradeSpeed();
                truck.UpgradeSize();
                if (!rebirth.BeginRebirth(0) || blessings.PendingCandidates.Count != 3)
                {
                    throw new InvalidOperationException("Level 10 rebirth did not create three candidates.");
                }

                HashSet<string> candidateIds = new HashSet<string>();
                for (int i = 0; i < blessings.PendingCandidates.Count; i++)
                {
                    candidateIds.Add(blessings.PendingCandidates[i].Id);
                }

                if (candidateIds.Count != 3)
                {
                    throw new InvalidOperationException("A blessing candidate was duplicated in one offer.");
                }

                if (!rebirth.CompleteRebirth(0, out RebirthResult firstResult))
                {
                    throw new InvalidOperationException("Level 10 rebirth could not be completed.");
                }

                if (player.Level != 1 || player.Exp != 0 || player.UpgradePoints != 0 || player.Soul != 50 || truck.GetStats().SpeedLevel != 0 || truck.GetStats().SizeLevel != 0)
                {
                    throw new InvalidOperationException("Rebirth reset or retained-state rules are incorrect.");
                }

                float expectedRewardMultiplier = 1f + config.Rebirth.RewardMultiplierPerMaxRebirth;
                if (!firstResult.IsMaximumTier || !Mathf.Approximately(rebirth.RewardMultiplier, expectedRewardMultiplier) || rebirth.MaxUnlockedTierIndex != 1 || blessings.TotalOwnedCount != 1)
                {
                    throw new InvalidOperationException("Maximum-tier rebirth progression is incorrect.");
                }

                player.RestoreState(20, 0, 50, 0, 0f, 0f);
                rebirth.BeginRebirth(0);
                rebirth.CompleteRebirth(0, out RebirthResult lowerResult);
                if (lowerResult.IsMaximumTier || !Mathf.Approximately(rebirth.RewardMultiplier, expectedRewardMultiplier) || rebirth.MaxUnlockedTierIndex != 1)
                {
                    throw new InvalidOperationException("Lower-tier rebirth changed the multiplier or maximum tier.");
                }

                string ownedId = firstResult.Blessing.Id;
                blessings.RestoreState(new List<OwnedBlessingData> { new OwnedBlessingData(ownedId, 1) }, new List<string> { ownedId });
                blessings.ChooseCandidate(0);
                if (blessings.GetOwnedCount(ownedId) != 2)
                {
                    throw new InvalidOperationException("Previously owned blessings cannot be acquired again.");
                }
            }
            finally
            {
                Object.DestroyImmediate(rebirthObject);
                Object.DestroyImmediate(blessingObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(truckObject);
            }

            GameObject fractionalPlayerObject = new GameObject("Reward Verification Player");
            try
            {
                PlayerState fractionalPlayer = fractionalPlayerObject.AddComponent<PlayerState>();
                fractionalPlayer.Initialize(config);
                for (int i = 0; i < 10; i++) fractionalPlayer.AddRewards(0, 1, 1.1f);
                if (fractionalPlayer.Soul != 11)
                {
                    throw new InvalidOperationException("Fractional reward carry did not preserve additive multiplier rewards.");
                }
            }
            finally
            {
                Object.DestroyImmediate(fractionalPlayerObject);
            }
        }

        private static void VerifySaveRestore(GameConfig config, BlessingCatalog catalog)
        {
            PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            GameObject source = new GameObject("Save Verification Source");
            GameObject restored = new GameObject("Save Verification Restored");

            try
            {
                ProgressSystems first = CreateProgressSystems(source, config, catalog, VerificationSaveKey);
                first.Player.RestoreState(10, 25, 77, 2, 0.3f, 0.4f);
                first.Truck.UpgradeSpeed();
                first.Truck.UpgradeSize();
                first.Truck.transform.position = new Vector3(12f, first.Truck.transform.position.y, -8f);
                first.Truck.transform.rotation = Quaternion.Euler(0f, 123f, 0f);
                first.Rebirth.BeginRebirth(0);
                List<string> pendingIds = first.Blessings.GetPendingCandidateIds();
                first.Save.Save();

                Object.DestroyImmediate(first.Save);
                ProgressSystems second = CreateProgressSystems(restored, config, catalog, VerificationSaveKey);
                if (second.Player.Level != 10 || second.Player.Exp != 25 || second.Player.Soul != 77 || second.Player.UpgradePoints != 2)
                {
                    throw new InvalidOperationException("Player progression was not restored.");
                }

                TruckController.TruckStats truck = second.Truck.GetStats();
                if (truck.SpeedLevel != 1 || truck.SizeLevel != 1 || !Mathf.Approximately(second.Truck.transform.position.x, 12f) || !Mathf.Approximately(second.Truck.transform.position.z, -8f))
                {
                    throw new InvalidOperationException("Truck progression or position was not restored.");
                }

                List<string> restoredIds = second.Blessings.GetPendingCandidateIds();
                if (!second.Rebirth.HasPendingRebirth || pendingIds.Count != restoredIds.Count)
                {
                    throw new InvalidOperationException("Pending rebirth candidates were not restored.");
                }

                for (int i = 0; i < pendingIds.Count; i++)
                {
                    if (pendingIds[i] != restoredIds[i])
                    {
                        throw new InvalidOperationException("Pending blessing candidates changed after loading.");
                    }
                }
            }
            finally
            {
                Object.DestroyImmediate(restored);
                Object.DestroyImmediate(source);
                PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            }
        }

        private static ProgressSystems CreateProgressSystems(GameObject root, GameConfig config, BlessingCatalog catalog, string saveKey)
        {
            TruckController truck = root.AddComponent<TruckController>();
            PlayerState player = root.AddComponent<PlayerState>();
            BlessingSystem blessings = root.AddComponent<BlessingSystem>();
            RebirthSystem rebirth = root.AddComponent<RebirthSystem>();
            TruckUpgradeSystem upgrades = root.AddComponent<TruckUpgradeSystem>();
            PlayerProgressSaveSystem save = root.AddComponent<PlayerProgressSaveSystem>();

            truck.Initialize(config);
            player.Initialize(config);
            blessings.SetCatalog(catalog);
            blessings.Initialize();
            rebirth.Initialize(config, player, truck, blessings);
            upgrades.Initialize(player, truck);
            save.SetSaveKeyForVerification(saveKey);
            save.Initialize(player, truck, rebirth, blessings, upgrades);
            return new ProgressSystems(truck, player, blessings, rebirth, save);
        }

        private static BlessingCatalog CreateBlessingCatalog()
        {
            List<BlessingDefinition> definitions = new List<BlessingDefinition>();
            BlessingGrade[] grades = { BlessingGrade.C, BlessingGrade.U, BlessingGrade.R, BlessingGrade.SR };
            for (int gradeIndex = 0; gradeIndex < grades.Length; gradeIndex++)
            {
                BlessingGrade grade = grades[gradeIndex];
                for (int index = 1; index <= 3; index++)
                {
                    string id = $"{grade.ToString().ToLowerInvariant()}_blessing_{index:00}";
                    string path = $"{DefinitionFolder}/{grade}_Blessing_{index:00}.asset";
                    BlessingDefinition definition = AssetDatabase.LoadAssetAtPath<BlessingDefinition>(path);
                    if (definition == null)
                    {
                        definition = ScriptableObject.CreateInstance<BlessingDefinition>();
                        AssetDatabase.CreateAsset(definition, path);
                        definition.Configure(id, $"{grade} 축복 {index}", grade, "효과는 다음 단계에서 설정됩니다.");
                    }

                    EditorUtility.SetDirty(definition);
                    definitions.Add(definition);
                }
            }

            BlessingCatalog catalog = AssetDatabase.LoadAssetAtPath<BlessingCatalog>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<BlessingCatalog>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }

            catalog.SetDefinitions(definitions);
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static RebirthUIController CreateUI(Transform canvas, Font font)
        {
            GameObject uiObject = CreateUIObject("Rebirth UI", canvas);
            Stretch(uiObject.GetComponent<RectTransform>());
            RebirthUIController controller = uiObject.AddComponent<RebirthUIController>();

            GameObject gameAreaObject = CreateUIObject("Rebirth Game Area", uiObject.transform);
            RectTransform gameArea = gameAreaObject.GetComponent<RectTransform>();
            Stretch(gameArea);

            Button openButton = CreateButton("Open Rebirth Button", gameArea, font, "환생", 22);
            SetRect(openButton.GetComponent<RectTransform>(), new Vector2(0.73f, 1f), Vector2.one, new Vector2(0f, -164f), new Vector2(-14f, -106f));

            GameObject panel = CreatePanel("Rebirth Panel", gameArea, new Color(0f, 0f, 0f, 0.7f));
            Stretch(panel.GetComponent<RectTransform>());

            GameObject box = CreatePanel("Rebirth Box", panel.transform, new Color(0.08f, 0.08f, 0.1f, 0.98f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(620f, 1050f);

            Text title = CreateText("Title", box.transform, font, "환생과 여신의 축복", 34, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, 20f, 55f, 24f);
            Text statusText = CreateText("Status", box.transform, font, string.Empty, 20, TextAnchor.MiddleCenter);
            SetTopRect(statusText.rectTransform, 80f, 42f, 20f);
            Text guideText = CreateText("Guide", box.transform, font, string.Empty, 19, TextAnchor.MiddleCenter);
            guideText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetTopRect(guideText.rectTransform, 128f, 82f, 28f);

            GameObject tierPanel = CreateUIObject("Tier Panel", box.transform);
            SetRect(tierPanel.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(28f, 145f), new Vector2(-28f, -225f));
            Button[] tierButtons = new Button[10];
            Text[] tierLabels = new Text[10];
            for (int i = 0; i < tierButtons.Length; i++)
            {
                int column = i % 2;
                int row = i / 2;
                float top = row * 0.2f;
                tierButtons[i] = CreateButton($"Tier {i + 1}", tierPanel.transform, font, string.Empty, 20);
                RectTransform rect = tierButtons[i].GetComponent<RectTransform>();
                SetRect(rect, new Vector2(column * 0.5f, 0.8f - top), new Vector2((column + 1) * 0.5f, 1f - top), new Vector2(6f, 7f), new Vector2(-6f, -7f));
                tierLabels[i] = tierButtons[i].GetComponentInChildren<Text>();
            }

            Button confirmButton = CreateButton("Confirm Rebirth Button", box.transform, font, "선택한 단계로 환생", 24);
            SetBottomRect(confirmButton.GetComponent<RectTransform>(), 72f, 58f, 30f);

            GameObject candidatePanel = CreateUIObject("Candidate Panel", box.transform);
            SetRect(candidatePanel.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(28f, 150f), new Vector2(-28f, -225f));
            Button[] candidateButtons = new Button[3];
            Text[] candidateLabels = new Text[3];
            for (int i = 0; i < candidateButtons.Length; i++)
            {
                float minY = 1f - (i + 1) / 3f;
                float maxY = 1f - i / 3f;
                candidateButtons[i] = CreateButton($"Blessing Candidate {i + 1}", candidatePanel.transform, font, string.Empty, 21);
                SetRect(candidateButtons[i].GetComponent<RectTransform>(), new Vector2(0f, minY), new Vector2(1f, maxY), new Vector2(6f, 9f), new Vector2(-6f, -9f));
                candidateLabels[i] = candidateButtons[i].GetComponentInChildren<Text>();
            }

            Button closeButton = CreateButton("Close Rebirth Button", box.transform, font, "닫기", 21);
            SetBottomRect(closeButton.GetComponent<RectTransform>(), 15f, 46f, 190f);

            GameObject confirmationPopup = CreateConfirmationPopup(panel.transform, font, out Text confirmationText, out Button confirmPopupButton, out Button cancelPopupButton);
            controller.SetReferences(gameArea, panel, tierPanel, candidatePanel, statusText, guideText, openButton, closeButton, confirmButton, confirmationPopup, confirmationText, confirmPopupButton, cancelPopupButton, tierButtons, tierLabels, candidateButtons, candidateLabels);
            panel.SetActive(false);
            return controller;
        }

        private static GameObject CreateConfirmationPopup(Transform parent, Font font, out Text confirmationText, out Button confirmPopupButton, out Button cancelPopupButton)
        {
            GameObject popup = CreatePanel("Rebirth Confirmation Popup", parent, new Color(0f, 0f, 0f, 0.78f));
            Stretch(popup.GetComponent<RectTransform>());

            GameObject box = CreatePanel("Confirmation Box", popup.transform, new Color(0.08f, 0.08f, 0.1f, 1f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(520f, 340f);

            Text title = CreateText("Confirmation Title", box.transform, font, "환생 확인", 30, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, 22f, 52f, 24f);
            confirmationText = CreateText("Confirmation Text", box.transform, font, string.Empty, 21, TextAnchor.MiddleCenter);
            confirmationText.horizontalOverflow = HorizontalWrapMode.Wrap;
            confirmationText.verticalOverflow = VerticalWrapMode.Overflow;
            SetTopRect(confirmationText.rectTransform, 82f, 140f, 32f);

            confirmPopupButton = CreateButton("Confirm Rebirth Popup Button", box.transform, font, "환생하기", 23);
            SetRect(confirmPopupButton.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.5f, 0f), new Vector2(26f, 26f), new Vector2(-8f, 88f));
            cancelPopupButton = CreateButton("Cancel Rebirth Popup Button", box.transform, font, "취소", 23);
            SetRect(cancelPopupButton.GetComponent<RectTransform>(), new Vector2(0.5f, 0f), new Vector2(1f, 0f), new Vector2(8f, 26f), new Vector2(-26f, 88f));

            popup.SetActive(false);
            return popup;
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

        private static void SetBottomRect(RectTransform rectTransform, float bottom, float height, float horizontalMargin)
        {
            SetRect(rectTransform, Vector2.zero, new Vector2(1f, 0f), new Vector2(horizontalMargin, bottom), new Vector2(-horizontalMargin, bottom + height));
        }

        private static void SetRect(RectTransform rectTransform, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            rectTransform.anchorMin = anchorMin;
            rectTransform.anchorMax = anchorMax;
            rectTransform.offsetMin = offsetMin;
            rectTransform.offsetMax = offsetMax;
        }

        private readonly struct ProgressSystems
        {
            public ProgressSystems(TruckController truck, PlayerState player, BlessingSystem blessings, RebirthSystem rebirth, PlayerProgressSaveSystem save)
            {
                Truck = truck;
                Player = player;
                Blessings = blessings;
                Rebirth = rebirth;
                Save = save;
            }

            public TruckController Truck { get; }
            public PlayerState Player { get; }
            public BlessingSystem Blessings { get; }
            public RebirthSystem Rebirth { get; }
            public PlayerProgressSaveSystem Save { get; }
        }
    }
}
