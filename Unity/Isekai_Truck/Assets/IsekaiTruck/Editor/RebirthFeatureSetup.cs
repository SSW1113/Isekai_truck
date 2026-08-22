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
            uiController.transform.SetAsLastSibling();
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
            GameUIController gameUI = Object.FindFirstObjectByType<GameUIController>();
            if (gameManager == null || sceneBlessings == null || sceneRebirth == null || sceneSave == null || sceneUI == null || gameUI == null)
            {
                throw new InvalidOperationException("Rebirth scene systems are missing.");
            }

            if (sceneUI.transform.parent != gameUI.transform.parent || sceneUI.transform.GetSiblingIndex() <= gameUI.transform.GetSiblingIndex())
            {
                throw new InvalidOperationException("Rebirth UI must render above the main HUD.");
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
                "gameArea", "rebirthPanel", "tierPanel", "statusText", "guideText",
                "openButton", "openButtonLabel", "availabilityIndicator", "closeButton", "confirmButton",
                "tierButtons", "tierLabels", "blessingSelectionUI"
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

            BlessingSelectionUI blessingSelectionUI = (BlessingSelectionUI)serializedUI.FindProperty("blessingSelectionUI").objectReferenceValue;
            Button rebirthButton = (Button)serializedUI.FindProperty("openButton").objectReferenceValue;
            SerializedObject serializedBlessingUI = new SerializedObject(blessingSelectionUI);
            if (serializedBlessingUI.FindProperty("overlay").objectReferenceValue == null ||
                serializedBlessingUI.FindProperty("cards").arraySize != 3 ||
                rebirthButton.GetComponent<CartoonButtonPressEffect>() == null ||
                !HudColorPalette.Matches(rebirthButton.GetComponent<Image>().color, HudColorPalette.Soul) ||
                sceneUI.transform.Find("Blessing Selection Overlay/Card Row/Blessing Card 1/Icon") == null ||
                sceneUI.transform.Find("Blessing Selection Overlay/Card Row/Blessing Card 2/Name Text") == null ||
                sceneUI.transform.Find("Blessing Selection Overlay/Card Row/Blessing Card 3/Description Text") == null)
            {
                throw new InvalidOperationException("Blessing selection card UI is incomplete.");
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
                int productionRequiredLevel = (i + 1) * 10;
                bool isDebugFirstTier = i == 0 && tier.RequiredLevel == 1;
                bool hasValidRequiredLevel = tier.RequiredLevel == productionRequiredLevel || isDebugFirstTier;
                if (!hasValidRequiredLevel || !Mathf.Approximately(tier.TotalWeight, 100f))
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

            GameObject metaProgressionHud = CreateUIObject("Meta Progression HUD", uiObject.transform);
            Stretch(metaProgressionHud.GetComponent<RectTransform>());
            Button openButton = CreateButton("Rebirth Button", metaProgressionHud.transform, font, "환생", 22);
            SetRect(openButton.GetComponent<RectTransform>(), new Vector2(0.8278f, 0.135f), new Vector2(0.9622f, 0.195f), Vector2.zero, Vector2.zero);
            Text openButtonLabel = openButton.GetComponentInChildren<Text>();
            GameObject availabilityIndicator = StyleRebirthEntryButton(openButton, font);

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

            Button closeButton = CreateButton("Close Rebirth Button", box.transform, font, "닫기", 21);
            SetBottomRect(closeButton.GetComponent<RectTransform>(), 15f, 46f, 190f);

            BlessingSelectionUI blessingSelectionUI = CreateBlessingSelectionUI(uiObject.transform, font);

            controller.SetReferences(
                gameArea,
                panel,
                tierPanel,
                statusText,
                guideText,
                openButton,
                openButtonLabel,
                availabilityIndicator,
                closeButton,
                confirmButton,
                tierButtons,
                tierLabels,
                blessingSelectionUI
            );
            panel.SetActive(false);
            return controller;
        }

        private static BlessingSelectionUI CreateBlessingSelectionUI(Transform parent, Font font)
        {
            BlessingSelectionUI selectionUI = parent.gameObject.AddComponent<BlessingSelectionUI>();
            GameObject overlay = CreatePanel("Blessing Selection Overlay", parent, new Color(0.01f, 0.015f, 0.025f, 0.58f));
            Stretch(overlay.GetComponent<RectTransform>());
            overlay.GetComponent<Image>().raycastTarget = true;

            Text title = CreateText("Title", overlay.transform, font, "여신의 축복 선택", 42, TextAnchor.MiddleCenter);
            SetRect(title.rectTransform, new Vector2(0.20f, 0.82f), new Vector2(0.80f, 0.91f), Vector2.zero, Vector2.zero);
            title.fontStyle = FontStyle.Bold;
            title.color = new Color(0.94f, 0.90f, 1f, 1f);

            GameObject cardContainer = CreateUIObject("Card Row", overlay.transform);
            RectTransform cardContainerRect = cardContainer.GetComponent<RectTransform>();
            SetRect(cardContainerRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cardContainerRect.sizeDelta = new Vector2(900f, 500f);
            cardContainerRect.anchoredPosition = new Vector2(0f, -30f);

            BlessingCardView[] cards = new BlessingCardView[3];
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i] = CreateBlessingCard(cardContainer.transform, font, i);
            }

            selectionUI.SetReferences(overlay, cards);
            overlay.SetActive(false);
            return selectionUI;
        }

        private static BlessingCardView CreateBlessingCard(Transform parent, Font font, int index)
        {
            Color cardColor = new Color(0.035f, 0.06f, 0.075f, 0.98f);
            Color borderColor = new Color(0.73f, 0.65f, 0.88f, 0.88f);
            GameObject cardObject = CreatePanel($"Blessing Card {index + 1}", parent, cardColor);
            RectTransform cardRect = cardObject.GetComponent<RectTransform>();
            SetRect(cardRect, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
            cardRect.sizeDelta = new Vector2(270f, 470f);
            cardRect.anchoredPosition = new Vector2((index - 1) * 302f, 0f);
            Image background = cardObject.GetComponent<Image>();
            background.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            background.type = Image.Type.Sliced;

            Outline border = cardObject.AddComponent<Outline>();
            border.effectColor = borderColor;
            border.effectDistance = new Vector2(2f, -2f);
            Shadow shadow = cardObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(0.22f, 0.12f, 0.20f, 0.30f);
            shadow.effectDistance = new Vector2(0f, -5f);

            GameObject hoverHighlight = CreatePanel("Hover Highlight", cardObject.transform, new Color(0.76f, 0.68f, 1f, 0f));
            RectTransform highlightRect = hoverHighlight.GetComponent<RectTransform>();
            SetRect(highlightRect, new Vector2(0.5f, 0.78f), new Vector2(0.5f, 0.78f), Vector2.zero, Vector2.zero);
            highlightRect.sizeDelta = new Vector2(190f, 190f);
            Image spotlight = hoverHighlight.GetComponent<Image>();
            spotlight.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            spotlight.raycastTarget = false;

            GameObject iconObject = CreatePanel("Icon", cardObject.transform, Color.white);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            SetRect(iconRect, new Vector2(0.5f, 0.77f), new Vector2(0.5f, 0.77f), Vector2.zero, Vector2.zero);
            iconRect.sizeDelta = new Vector2(108f, 108f);
            Image icon = iconObject.GetComponent<Image>();
            icon.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            icon.type = Image.Type.Simple;
            icon.raycastTarget = false;

            GameObject iconMark = CreatePanel("Icon Mark", iconObject.transform, new Color(1f, 1f, 1f, 0.82f));
            RectTransform iconMarkRect = iconMark.GetComponent<RectTransform>();
            SetRect(iconMarkRect, new Vector2(0.24f, 0.44f), new Vector2(0.76f, 0.56f), Vector2.zero, Vector2.zero);
            iconMarkRect.localRotation = Quaternion.Euler(0f, 0f, 45f + index * 30f);
            iconMark.GetComponent<Image>().raycastTarget = false;

            Text gradeText = CreateText("Grade Text", cardObject.transform, font, string.Empty, 18, TextAnchor.MiddleCenter);
            SetRect(gradeText.rectTransform, new Vector2(0.16f, 0.60f), new Vector2(0.84f, 0.67f), Vector2.zero, Vector2.zero);
            gradeText.fontStyle = FontStyle.Bold;

            Text nameText = CreateText("Name Text", cardObject.transform, font, string.Empty, 24, TextAnchor.MiddleCenter);
            SetRect(nameText.rectTransform, new Vector2(0.08f, 0.44f), new Vector2(0.92f, 0.59f), Vector2.zero, Vector2.zero);
            nameText.fontStyle = FontStyle.Bold;
            nameText.color = new Color(0.96f, 0.93f, 1f, 1f);
            nameText.horizontalOverflow = HorizontalWrapMode.Wrap;

            Text descriptionText = CreateText("Description Text", cardObject.transform, font, string.Empty, 18, TextAnchor.UpperCenter);
            SetRect(descriptionText.rectTransform, new Vector2(0.10f, 0.12f), new Vector2(0.90f, 0.42f), Vector2.zero, Vector2.zero);
            descriptionText.color = new Color(0.78f, 0.82f, 0.86f, 1f);
            descriptionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            descriptionText.verticalOverflow = VerticalWrapMode.Truncate;

            Text ownedText = CreateText("Owned Text", cardObject.transform, font, string.Empty, 16, TextAnchor.MiddleCenter);
            SetRect(ownedText.rectTransform, new Vector2(0.12f, 0.04f), new Vector2(0.88f, 0.10f), Vector2.zero, Vector2.zero);
            ownedText.color = new Color(0.64f, 0.70f, 0.74f, 1f);

            BlessingCardView cardView = cardObject.AddComponent<BlessingCardView>();
            cardView.SetReferences(cardRect, iconRect, background, icon, spotlight, border, gradeText, nameText, descriptionText, ownedText);
            return cardView;
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

        private static GameObject StyleRebirthEntryButton(Button button, Font font)
        {
            Image image = button.GetComponent<Image>();
            image.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
            image.type = Image.Type.Sliced;
            image.color = HudColorPalette.Soul;

            Outline outline = button.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(HudColorPalette.SoulDepth.r, HudColorPalette.SoulDepth.g, HudColorPalette.SoulDepth.b, 0.84f);
            outline.effectDistance = new Vector2(2f, -2f);

            Shadow shadow = button.gameObject.AddComponent<Shadow>();
            shadow.effectColor = new Color(HudColorPalette.SoulDepth.r, HudColorPalette.SoulDepth.g, HudColorPalette.SoulDepth.b, 0.32f);
            shadow.effectDistance = new Vector2(0f, -4f);

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.78f, 0.86f, 0.86f, 1f);
            colors.selectedColor = colors.highlightedColor;
            colors.disabledColor = new Color(0.62f, 0.58f, 0.60f, 0.72f);
            colors.colorMultiplier = 1f;
            colors.fadeDuration = 0.12f;
            button.colors = colors;

            CartoonButtonPressEffect interaction = button.gameObject.AddComponent<CartoonButtonPressEffect>();
            interaction.Configure((RectTransform)button.transform, null, 1.04f, 0.97f, 1.2f);

            Text label = button.GetComponentInChildren<Text>();
            label.font = font;
            label.color = Color.white;
            label.fontStyle = FontStyle.Bold;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 14;
            label.resizeTextMaxSize = 24;
            label.text = "환생";

            GameObject soulMark = CreatePanel("Soul Mark", button.transform, new Color(1f, 0.92f, 0.62f, 1f));
            RectTransform soulMarkRect = soulMark.GetComponent<RectTransform>();
            SetRect(soulMarkRect, new Vector2(0.10f, 0.26f), new Vector2(0.30f, 0.74f), Vector2.zero, Vector2.zero);
            soulMark.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            soulMark.GetComponent<Image>().raycastTarget = false;

            SetRect(label.rectTransform, new Vector2(0.28f, 0f), new Vector2(0.94f, 1f), Vector2.zero, Vector2.zero);

            GameObject availabilityIndicator = CreatePanel("Availability Indicator", button.transform, new Color(1f, 0.82f, 0.26f, 1f));
            RectTransform indicatorRect = availabilityIndicator.GetComponent<RectTransform>();
            SetRect(indicatorRect, new Vector2(0.90f, 0.88f), new Vector2(0.90f, 0.88f), Vector2.zero, Vector2.zero);
            indicatorRect.sizeDelta = new Vector2(22f, 22f);
            availabilityIndicator.GetComponent<Image>().sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            availabilityIndicator.GetComponent<Image>().raycastTarget = false;
            Text indicatorText = CreateText("Indicator Text", availabilityIndicator.transform, font, "!", 16, TextAnchor.MiddleCenter);
            Stretch(indicatorText.rectTransform);
            indicatorText.color = new Color(0.34f, 0.16f, 0.37f, 1f);
            availabilityIndicator.SetActive(false);
            return availabilityIndicator;
        }

        internal static void ApplyPaletteToExistingUI(Transform canvas)
        {
            Transform buttonTransform = canvas.Find("Rebirth UI/Meta Progression HUD/Rebirth Button");
            if (buttonTransform == null)
            {
                return;
            }

            Button button = buttonTransform.GetComponent<Button>();
            Image image = buttonTransform.GetComponent<Image>();
            Outline outline = buttonTransform.GetComponent<Outline>();
            Shadow[] shadows = buttonTransform.GetComponents<Shadow>();
            image.color = HudColorPalette.Soul;
            if (outline != null)
            {
                outline.effectColor = new Color(HudColorPalette.SoulDepth.r, HudColorPalette.SoulDepth.g, HudColorPalette.SoulDepth.b, 0.84f);
            }

            for (int i = 0; i < shadows.Length; i++)
            {
                if (shadows[i].GetType() == typeof(Shadow))
                {
                    shadows[i].effectColor = new Color(HudColorPalette.SoulDepth.r, HudColorPalette.SoulDepth.g, HudColorPalette.SoulDepth.b, 0.32f);
                }
            }

            ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(0.84f, 0.94f, 0.94f, 1f);
            colors.pressedColor = new Color(0.78f, 0.86f, 0.86f, 1f);
            colors.selectedColor = colors.highlightedColor;
            button.colors = colors;
            EditorUtility.SetDirty(button);
            EditorUtility.SetDirty(image);
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
