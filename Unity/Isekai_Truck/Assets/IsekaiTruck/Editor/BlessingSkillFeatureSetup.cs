using System;
using IsekaiTruck.Blessings;
using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Monsters;
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
    public static class BlessingSkillFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string CatalogPath = "Assets/IsekaiTruck/Blessings/BlessingCatalog.asset";
        private const string DefinitionFolder = "Assets/IsekaiTruck/Blessings/Definitions";
        private const string VerificationSaveKey = "IsekaiTruck.BlessingSkillVerification";

        [MenuItem("Isekai Truck/Setup Blessing Skill Feature")]
        public static void Setup()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            BlessingCatalog catalog = AssetDatabase.LoadAssetAtPath<BlessingCatalog>(CatalogPath);
            if (config == null || catalog == null)
            {
                throw new InvalidOperationException("GameConfig or BlessingCatalog was not found.");
            }

            ConfigureBlessings();
            EditorUtility.SetDirty(config);

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            BlessingSystem blessingSystem = Object.FindFirstObjectByType<BlessingSystem>();
            Canvas canvas = Object.FindFirstObjectByType<Canvas>();
            if (gameManager == null || blessingSystem == null || canvas == null)
            {
                throw new InvalidOperationException("Main scene blessing systems or Canvas were not found.");
            }

            GameObject systemsObject = blessingSystem.gameObject;
            BlessingLoadoutSystem loadoutSystem = GetOrAdd<BlessingLoadoutSystem>(systemsObject);
            BlessingDismantleSystem dismantleSystem = GetOrAdd<BlessingDismantleSystem>(systemsObject);
            BlessingEffectSystem effectSystem = GetOrAdd<BlessingEffectSystem>(systemsObject);
            BlessingInput blessingInput = GetOrAdd<BlessingInput>(systemsObject);

            Transform existingUI = canvas.transform.Find("Blessing Inventory UI");
            if (existingUI != null)
            {
                Object.DestroyImmediate(existingUI.gameObject);
            }

            Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            BlessingInventoryUIController inventoryUI = CreateUI(canvas.transform, font);
            gameManager.SetBlessingSkillSystems(loadoutSystem, dismantleSystem, effectSystem, blessingInput, inventoryUI);
            MainHudLayoutSetup.ApplyToLoadedScene();
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "축복 장착, 분해, 액티브/패시브 효과를 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            BlessingCatalog catalog = AssetDatabase.LoadAssetAtPath<BlessingCatalog>(CatalogPath);
            VerifyDefinitions(config, catalog);

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            BlessingInventoryUIController inventoryUI = Object.FindFirstObjectByType<BlessingInventoryUIController>();
            BlessingLoadoutSystem loadout = Object.FindFirstObjectByType<BlessingLoadoutSystem>();
            BlessingDismantleSystem dismantle = Object.FindFirstObjectByType<BlessingDismantleSystem>();
            BlessingEffectSystem effects = Object.FindFirstObjectByType<BlessingEffectSystem>();
            BlessingInput input = Object.FindFirstObjectByType<BlessingInput>();
            if (gameManager == null || inventoryUI == null || loadout == null || dismantle == null || effects == null || input == null)
            {
                throw new InvalidOperationException("Blessing skill scene systems are missing.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            string[] managerReferences = { "blessingLoadoutSystem", "blessingDismantleSystem", "blessingEffectSystem", "blessingInput", "blessingInventoryUIController" };
            for (int i = 0; i < managerReferences.Length; i++)
            {
                if (serializedGameManager.FindProperty(managerReferences[i]).objectReferenceValue == null)
                {
                    throw new InvalidOperationException($"GameManager reference is missing: {managerReferences[i]}");
                }
            }

            SerializedObject serializedUI = new SerializedObject(inventoryUI);
            string[] uiReferences =
            {
                "gameArea", "inventoryPanel", "selectionText", "openButton", "closeButton", "equipButton",
                "unequipButton", "dismantleButton", "slotButtons", "slotLabels", "activeSlotLabels", "inventoryButtons", "inventoryLabels"
            };
            for (int i = 0; i < uiReferences.Length; i++)
            {
                SerializedProperty property = serializedUI.FindProperty(uiReferences[i]);
                bool isMissing = property == null || property.isArray && property.arraySize == 0 || !property.isArray && property.objectReferenceValue == null;
                if (isMissing)
                {
                    throw new InvalidOperationException($"BlessingInventoryUIController reference is missing: {uiReferences[i]}");
                }
            }

            VerifyRuntime(config, catalog);
            VerifySave(config, catalog);
            Debug.Log("Blessing skill feature verification passed.");
        }

        public static void VerifyAll()
        {
            Verify();
            RebirthFeatureSetup.Verify();
            RebirthFeatureSetup.VerifyRegressions();
            Debug.Log("Blessing skill and all existing feature verifications passed.");
        }

        private static void VerifyDefinitions(GameConfig config, BlessingCatalog catalog)
        {
            if (config.Blessing.SlotCount != 3 || config.Blessing.CDismantleSoul != 200 || config.Blessing.UDismantleSoul != 300 || config.Blessing.RDismantleSoul != 400 || config.Blessing.SrDismantleSoul != 600)
            {
                throw new InvalidOperationException("Blessing slot or dismantle configuration is incorrect.");
            }

            if (catalog.Definitions.Count != 12)
            {
                throw new InvalidOperationException("Blessing catalog must contain twelve definitions.");
            }

            VerifyDefinition(catalog, "c_blessing_01", "위압감", BlessingGrade.C, BlessingActivationType.Passive, BlessingEffectType.MonsterSlow, 0.8f);
            VerifyDefinition(catalog, "c_blessing_02", "천리안", BlessingGrade.C, BlessingActivationType.Active, BlessingEffectType.VisionBoost, 1.5f);
            VerifyDefinition(catalog, "c_blessing_03", "스턴건", BlessingGrade.C, BlessingActivationType.Passive, BlessingEffectType.PeriodicStun, 1f);
            VerifyDefinition(catalog, "u_blessing_01", "Test1", BlessingGrade.U, BlessingActivationType.Passive, BlessingEffectType.ExperienceGain, 1.1f);
            VerifyDefinition(catalog, "u_blessing_02", "Test2", BlessingGrade.U, BlessingActivationType.Passive, BlessingEffectType.ExperienceGain, 1.1f);
            VerifyDefinition(catalog, "u_blessing_03", "Test3", BlessingGrade.U, BlessingActivationType.Passive, BlessingEffectType.ExperienceGain, 1.1f);
            VerifyDefinition(catalog, "r_blessing_01", "부스터", BlessingGrade.R, BlessingActivationType.Active, BlessingEffectType.TruckBoost, 1.5f);
            VerifyDefinition(catalog, "r_blessing_02", "스피드업", BlessingGrade.R, BlessingActivationType.Passive, BlessingEffectType.TruckSpeed, 1.1f);
            VerifyDefinition(catalog, "r_blessing_03", "벌크업", BlessingGrade.R, BlessingActivationType.Passive, BlessingEffectType.TruckSize, 1.1f);
            VerifyDefinition(catalog, "sr_blessing_01", "시간 정지", BlessingGrade.SR, BlessingActivationType.Active, BlessingEffectType.TimeStop, 1f);
            VerifyDefinition(catalog, "sr_blessing_02", "스피드업+", BlessingGrade.SR, BlessingActivationType.Passive, BlessingEffectType.TruckSpeed, 1.2f);
            VerifyDefinition(catalog, "sr_blessing_03", "벌크업+", BlessingGrade.SR, BlessingActivationType.Passive, BlessingEffectType.TruckSize, 1.2f);
        }

        private static void VerifyDefinition(BlessingCatalog catalog, string id, string displayName, BlessingGrade grade, BlessingActivationType activationType, BlessingEffectType effectType, float value)
        {
            BlessingDefinition definition = catalog.FindById(id);
            if (definition == null || definition.DisplayName != displayName || definition.Grade != grade || definition.ActivationType != activationType || definition.EffectType != effectType || !Mathf.Approximately(definition.EffectValue, value))
            {
                throw new InvalidOperationException($"Blessing definition is incorrect: {id}");
            }
        }

        private static void VerifyRuntime(GameConfig config, BlessingCatalog catalog)
        {
            GameObject truckObject = new GameObject("Blessing Verification Truck");
            GameObject playerObject = new GameObject("Blessing Verification Player");
            GameObject cameraObject = new GameObject("Blessing Verification Camera", typeof(UnityEngine.Camera), typeof(CameraController));
            GameObject monsterObject = new GameObject("Blessing Verification Monsters");
            GameObject systemsObject = new GameObject("Blessing Verification Systems");

            try
            {
                TruckController truck = truckObject.AddComponent<TruckController>();
                PlayerState player = playerObject.AddComponent<PlayerState>();
                MonsterManager monsterManager = monsterObject.AddComponent<MonsterManager>();
                BlessingSystem blessings = systemsObject.AddComponent<BlessingSystem>();
                BlessingLoadoutSystem loadout = systemsObject.AddComponent<BlessingLoadoutSystem>();
                BlessingDismantleSystem dismantle = systemsObject.AddComponent<BlessingDismantleSystem>();
                BlessingEffectSystem effects = systemsObject.AddComponent<BlessingEffectSystem>();

                truck.Initialize(config);
                player.Initialize(config);
                blessings.SetCatalog(catalog);
                blessings.Initialize();
                loadout.Initialize(config, blessings);
                dismantle.Initialize(config, blessings, loadout, player);
                effects.Initialize(loadout, truck, cameraObject.GetComponent<CameraController>(), monsterManager);

                blessings.AddOwnedForVerification("u_blessing_01", 3);
                loadout.TryEquip(0, "u_blessing_01");
                loadout.TryEquip(1, "u_blessing_01");
                loadout.TryEquip(2, "u_blessing_01");
                if (!Mathf.Approximately(effects.ExperienceMultiplier, 1.1f * 1.1f * 1.1f))
                {
                    throw new InvalidOperationException("Duplicate blessing effects are not multiplied.");
                }

                if (dismantle.TryDismantle("u_blessing_01", out BlessingDismantleResult blockedResult))
                {
                    throw new InvalidOperationException("An equipped blessing copy was dismantled.");
                }

                loadout.Unequip(2);
                if (!dismantle.TryDismantle("u_blessing_01", out BlessingDismantleResult dismantleResult) || dismantleResult.Soul != 300 || player.Soul != 300)
                {
                    throw new InvalidOperationException("U blessing dismantle reward is incorrect.");
                }

                loadout.Unequip(0);
                loadout.Unequip(1);
                blessings.AddOwnedForVerification("r_blessing_02", 3);
                loadout.TryEquip(0, "r_blessing_02");
                loadout.TryEquip(1, "r_blessing_02");
                loadout.TryEquip(2, "r_blessing_02");
                float expectedSpeed = config.Truck.BaseMaxSpeed * 1.1f * 1.1f * 1.1f;
                if (!Mathf.Approximately(truck.GetStats().MaxSpeed, expectedSpeed))
                {
                    throw new InvalidOperationException("Truck speed blessing stacking is incorrect.");
                }

                loadout.Unequip(0);
                loadout.Unequip(1);
                loadout.Unequip(2);
                blessings.AddOwnedForVerification("r_blessing_01", 1);
                loadout.TryEquip(0, "r_blessing_01");
                if (!effects.TryActivate(0) || !Mathf.Approximately(truck.transform.localScale.x, 1.5f) || effects.TryActivate(0))
                {
                    throw new InvalidOperationException("Booster activation or reactivation blocking is incorrect.");
                }

                loadout.Unequip(0);
                blessings.AddOwnedForVerification("sr_blessing_01", 1);
                loadout.TryEquip(0, "sr_blessing_01");
                effects.TryActivate(0);
                if (!effects.IsWorldTimeStopped)
                {
                    throw new InvalidOperationException("Time stop did not pause the world state.");
                }

                GameObject directMonsterObject = new GameObject("Direct Monster Verification");
                directMonsterObject.transform.SetParent(monsterObject.transform, false);
                directMonsterObject.transform.position = new Vector3(5f, 0f, 0f);
                MonsterController directMonster = directMonsterObject.AddComponent<MonsterController>();
                MonsterData monsterData = new MonsterData("verify", "Verify", "#ffffff", Color.white, 1f, 1f, 0f, 0, 0, 1f);
                directMonster.Initialize(monsterData, truck.transform, 0f, config.ReferenceFrameRate);
                Vector3 startPosition = directMonster.transform.position;
                directMonster.UpdateMonster(0f, 0f, 0f, 1f, 1f / config.ReferenceFrameRate, 20f, 0.8f, false);
                if (!Mathf.Approximately(Vector3.Distance(startPosition, directMonster.transform.position), 0.16f))
                {
                    throw new InvalidOperationException("Monster area slow multiplier is incorrect.");
                }

                directMonster.ApplyStun(2f);
                startPosition = directMonster.transform.position;
                directMonster.UpdateMonster(16f, 0f, 0f, 1f, 1f, 20f, 0.8f, false);
                if (directMonster.transform.position != startPosition || !directMonster.IsStunned)
                {
                    throw new InvalidOperationException("Monster stun did not block movement.");
                }
            }
            finally
            {
                Object.DestroyImmediate(systemsObject);
                Object.DestroyImmediate(monsterObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifySave(GameConfig config, BlessingCatalog catalog)
        {
            PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            GameObject source = new GameObject("Blessing Save Source");
            GameObject restored = new GameObject("Blessing Save Restored");

            try
            {
                SaveSystems first = CreateSaveSystems(source, config, catalog);
                first.Blessings.AddOwnedForVerification("c_blessing_01", 2);
                first.Loadout.TryEquip(0, "c_blessing_01");
                first.Loadout.TryEquip(1, "c_blessing_01");
                first.Save.Save();
                Object.DestroyImmediate(first.Save);

                SaveSystems second = CreateSaveSystems(restored, config, catalog);
                if (second.Blessings.GetOwnedCount("c_blessing_01") != 2 || second.Loadout.GetEquipped(0)?.Id != "c_blessing_01" || second.Loadout.GetEquipped(1)?.Id != "c_blessing_01")
                {
                    throw new InvalidOperationException("Blessing loadout was not restored from save data.");
                }
            }
            finally
            {
                Object.DestroyImmediate(restored);
                Object.DestroyImmediate(source);
                PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            }
        }

        private static SaveSystems CreateSaveSystems(GameObject root, GameConfig config, BlessingCatalog catalog)
        {
            TruckController truck = root.AddComponent<TruckController>();
            PlayerState player = root.AddComponent<PlayerState>();
            BlessingSystem blessings = root.AddComponent<BlessingSystem>();
            BlessingLoadoutSystem loadout = root.AddComponent<BlessingLoadoutSystem>();
            RebirthSystem rebirth = root.AddComponent<RebirthSystem>();
            TruckUpgradeSystem upgrades = root.AddComponent<TruckUpgradeSystem>();
            PlayerProgressSaveSystem save = root.AddComponent<PlayerProgressSaveSystem>();

            truck.Initialize(config);
            player.Initialize(config);
            blessings.SetCatalog(catalog);
            blessings.Initialize();
            loadout.Initialize(config, blessings);
            rebirth.Initialize(config, player, truck, blessings);
            upgrades.Initialize(player, truck);
            save.SetSaveKeyForVerification(VerificationSaveKey);
            save.Initialize(player, truck, rebirth, blessings, loadout, upgrades);
            return new SaveSystems(blessings, loadout, save);
        }

        private static void ConfigureBlessings()
        {
            Configure("C_Blessing_01.asset", "c_blessing_01", "위압감", BlessingGrade.C, BlessingActivationType.Passive, BlessingEffectType.MonsterSlow, 0.8f, 0f, 0f, 20f, "트럭 주변 몬스터의 이동 속도가 20% 감소합니다.");
            Configure("C_Blessing_02.asset", "c_blessing_02", "천리안", BlessingGrade.C, BlessingActivationType.Active, BlessingEffectType.VisionBoost, 1.5f, 10f, 0f, 0f, "10초 동안 시야가 1.5배 넓어집니다.");
            Configure("C_Blessing_03.asset", "c_blessing_03", "스턴건", BlessingGrade.C, BlessingActivationType.Passive, BlessingEffectType.PeriodicStun, 1f, 2f, 5f, 20f, "5초마다 주변 몬스터 하나를 2초 동안 마비시킵니다.");
            Configure("U_Blessing_01.asset", "u_blessing_01", "Test1", BlessingGrade.U, BlessingActivationType.Passive, BlessingEffectType.ExperienceGain, 1.1f, 0f, 0f, 0f, "경험치 획득량이 1.1배 증가합니다.");
            Configure("U_Blessing_02.asset", "u_blessing_02", "Test2", BlessingGrade.U, BlessingActivationType.Passive, BlessingEffectType.ExperienceGain, 1.1f, 0f, 0f, 0f, "경험치 획득량이 1.1배 증가합니다.");
            Configure("U_Blessing_03.asset", "u_blessing_03", "Test3", BlessingGrade.U, BlessingActivationType.Passive, BlessingEffectType.ExperienceGain, 1.1f, 0f, 0f, 0f, "경험치 획득량이 1.1배 증가합니다.");
            Configure("R_Blessing_01.asset", "r_blessing_01", "부스터", BlessingGrade.R, BlessingActivationType.Active, BlessingEffectType.TruckBoost, 1.5f, 5f, 0f, 0f, "5초 동안 트럭의 속도와 크기가 1.5배 증가합니다.");
            Configure("R_Blessing_02.asset", "r_blessing_02", "스피드업", BlessingGrade.R, BlessingActivationType.Passive, BlessingEffectType.TruckSpeed, 1.1f, 0f, 0f, 0f, "트럭의 속도가 1.1배 증가합니다.");
            Configure("R_Blessing_03.asset", "r_blessing_03", "벌크업", BlessingGrade.R, BlessingActivationType.Passive, BlessingEffectType.TruckSize, 1.1f, 0f, 0f, 0f, "트럭의 크기가 1.1배 증가합니다.");
            Configure("SR_Blessing_01.asset", "sr_blessing_01", "시간 정지", BlessingGrade.SR, BlessingActivationType.Active, BlessingEffectType.TimeStop, 1f, 9f, 0f, 0f, "9초 동안 트럭을 제외한 게임 오브젝트의 시간을 정지합니다.");
            Configure("SR_Blessing_02.asset", "sr_blessing_02", "스피드업+", BlessingGrade.SR, BlessingActivationType.Passive, BlessingEffectType.TruckSpeed, 1.2f, 0f, 0f, 0f, "트럭의 속도가 1.2배 증가합니다.");
            Configure("SR_Blessing_03.asset", "sr_blessing_03", "벌크업+", BlessingGrade.SR, BlessingActivationType.Passive, BlessingEffectType.TruckSize, 1.2f, 0f, 0f, 0f, "트럭의 크기가 1.2배 증가합니다.");
        }

        private static void Configure(string fileName, string id, string displayName, BlessingGrade grade, BlessingActivationType activationType, BlessingEffectType effectType, float value, float duration, float interval, float radius, string description)
        {
            BlessingDefinition definition = AssetDatabase.LoadAssetAtPath<BlessingDefinition>($"{DefinitionFolder}/{fileName}");
            if (definition == null)
            {
                throw new InvalidOperationException($"Blessing definition was not found: {fileName}");
            }

            definition.Configure(id, displayName, grade, activationType, effectType, value, duration, interval, radius, description);
            EditorUtility.SetDirty(definition);
        }

        private static BlessingInventoryUIController CreateUI(Transform canvas, Font font)
        {
            GameObject uiObject = CreateUIObject("Blessing Inventory UI", canvas);
            Stretch(uiObject.GetComponent<RectTransform>());
            BlessingInventoryUIController controller = uiObject.AddComponent<BlessingInventoryUIController>();

            GameObject gameAreaObject = CreateUIObject("Blessing Game Area", uiObject.transform);
            RectTransform gameArea = gameAreaObject.GetComponent<RectTransform>();
            Stretch(gameArea);

            Button openButton = CreateButton("Open Blessing Button", gameArea, font, "축복", 21);
            SetRect(openButton.GetComponent<RectTransform>(), new Vector2(0.73f, 1f), Vector2.one, new Vector2(0f, -226f), new Vector2(-14f, -170f));

            GameObject activeHud = CreatePanel("Active Blessing Slots", gameArea, new Color(0f, 0f, 0f, 0.55f));
            activeHud.GetComponent<Image>().raycastTarget = false;
            SetRect(activeHud.GetComponent<RectTransform>(), Vector2.zero, new Vector2(0.72f, 0f), new Vector2(14f, 14f), new Vector2(0f, 145f));
            activeHud.SetActive(false);
            Text[] activeSlotLabels = new Text[3];
            for (int i = 0; i < activeSlotLabels.Length; i++)
            {
                activeSlotLabels[i] = CreateText($"Active Slot {i + 1}", activeHud.transform, font, $"{i + 1}  비어 있음", 17, TextAnchor.MiddleLeft);
                SetRect(activeSlotLabels[i].rectTransform, new Vector2(0f, 1f - (i + 1) / 3f), new Vector2(1f, 1f - i / 3f), new Vector2(12f, 0f), new Vector2(-8f, 0f));
            }

            GameObject panel = CreatePanel("Blessing Inventory Panel", gameArea, new Color(0f, 0f, 0f, 0.72f));
            Stretch(panel.GetComponent<RectTransform>());
            GameObject box = CreatePanel("Blessing Inventory Box", panel.transform, new Color(0.08f, 0.08f, 0.1f, 0.98f));
            RectTransform boxRect = box.GetComponent<RectTransform>();
            boxRect.anchorMin = new Vector2(0.5f, 0.5f);
            boxRect.anchorMax = new Vector2(0.5f, 0.5f);
            boxRect.pivot = new Vector2(0.5f, 0.5f);
            boxRect.sizeDelta = new Vector2(720f, 1120f);

            Text title = CreateText("Title", box.transform, font, "여신의 축복", 34, TextAnchor.MiddleCenter);
            SetTopRect(title.rectTransform, 18f, 50f, 20f);

            Button[] slotButtons = new Button[3];
            Text[] slotLabels = new Text[3];
            for (int i = 0; i < slotButtons.Length; i++)
            {
                slotButtons[i] = CreateButton($"Blessing Slot {i + 1}", box.transform, font, string.Empty, 19);
                SetRect(slotButtons[i].GetComponent<RectTransform>(), new Vector2(i / 3f, 1f), new Vector2((i + 1) / 3f, 1f), new Vector2(9f, -190f), new Vector2(-9f, -82f));
                slotLabels[i] = slotButtons[i].GetComponentInChildren<Text>();
            }

            Text selectionText = CreateText("Selection", box.transform, font, "장착하거나 분해할 축복을 선택하세요.", 19, TextAnchor.MiddleCenter);
            selectionText.horizontalOverflow = HorizontalWrapMode.Wrap;
            SetTopRect(selectionText.rectTransform, 205f, 105f, 28f);

            GameObject inventoryArea = CreateUIObject("Inventory Grid", box.transform);
            SetRect(inventoryArea.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, new Vector2(22f, 190f), new Vector2(-22f, -330f));
            Button[] inventoryButtons = new Button[12];
            Text[] inventoryLabels = new Text[12];
            for (int i = 0; i < inventoryButtons.Length; i++)
            {
                int column = i % 3;
                int row = i / 3;
                float minY = 1f - (row + 1) / 4f;
                float maxY = 1f - row / 4f;
                inventoryButtons[i] = CreateButton($"Inventory Blessing {i + 1}", inventoryArea.transform, font, string.Empty, 17);
                SetRect(inventoryButtons[i].GetComponent<RectTransform>(), new Vector2(column / 3f, minY), new Vector2((column + 1) / 3f, maxY), new Vector2(6f, 6f), new Vector2(-6f, -6f));
                inventoryLabels[i] = inventoryButtons[i].GetComponentInChildren<Text>();
            }

            Button equipButton = CreateButton("Equip Button", box.transform, font, "장착", 21);
            SetRect(equipButton.GetComponent<RectTransform>(), new Vector2(0f, 0f), new Vector2(0.33f, 0f), new Vector2(24f, 78f), new Vector2(-6f, 138f));
            Button unequipButton = CreateButton("Unequip Button", box.transform, font, "해제", 21);
            SetRect(unequipButton.GetComponent<RectTransform>(), new Vector2(0.33f, 0f), new Vector2(0.66f, 0f), new Vector2(6f, 78f), new Vector2(-6f, 138f));
            Button dismantleButton = CreateButton("Dismantle Button", box.transform, font, "분해", 21);
            SetRect(dismantleButton.GetComponent<RectTransform>(), new Vector2(0.66f, 0f), new Vector2(1f, 0f), new Vector2(6f, 78f), new Vector2(-24f, 138f));
            Button closeButton = CreateButton("Close Button", box.transform, font, "닫기", 21);
            SetBottomRect(closeButton.GetComponent<RectTransform>(), 14f, 48f, 220f);

            controller.SetReferences(gameArea, panel, selectionText, openButton, closeButton, equipButton, unequipButton, dismantleButton, slotButtons, slotLabels, activeSlotLabels, inventoryButtons, inventoryLabels);
            panel.SetActive(false);
            return controller;
        }

        private static T GetOrAdd<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component == null ? gameObject.AddComponent<T>() : component;
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
            StretchWithOffsets(text.rectTransform, 7f, 7f, 4f, 4f);
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

        private readonly struct SaveSystems
        {
            public SaveSystems(BlessingSystem blessings, BlessingLoadoutSystem loadout, PlayerProgressSaveSystem save)
            {
                Blessings = blessings;
                Loadout = loadout;
                Save = save;
            }

            public BlessingSystem Blessings { get; }
            public BlessingLoadoutSystem Loadout { get; }
            public PlayerProgressSaveSystem Save { get; }
        }
    }
}
