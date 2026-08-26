using System;
using System.Collections.Generic;
using IsekaiTruck.Blessings;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Enemies;
using IsekaiTruck.Player;
using IsekaiTruck.Rebirth;
using IsekaiTruck.Save;
using IsekaiTruck.Spawn;
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
    public static class EnemyFeatureSetup
    {
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string BlessingCatalogPath = "Assets/IsekaiTruck/Blessings/BlessingCatalog.asset";
        private const string EnemyFolder = "Assets/IsekaiTruck/Enemies";
        private const string EnemyPrefabFolder = "Assets/IsekaiTruck/Prefabs/Enemies";
        private const string MaterialFolder = "Assets/IsekaiTruck/Materials";
        private const string EnemyCatalogPath = EnemyFolder + "/EnemyPrefabCatalog.asset";
        private const string BasicEnemyPrefabPath = EnemyPrefabFolder + "/BasicEnemy.prefab";
        private const string BasicEnemyMaterialPath = MaterialFolder + "/BasicEnemy.mat";
        private const string VerificationSaveKey = "IsekaiTruck.EnemyFeatureVerification";

        [MenuItem("Isekai Truck/Setup Enemy Feature")]
        public static void Setup()
        {
            EnsureFolder(EnemyFolder);
            EnsureFolder(EnemyPrefabFolder);
            EnsureFolder(MaterialFolder);

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            if (config == null)
            {
                throw new InvalidOperationException("GameConfig was not found.");
            }

            EnsureConfigDefaults(config);
            EnemyController basicEnemy = GetOrCreateBasicEnemyPrefab();
            EnemyPrefabCatalog catalog = GetOrCreateCatalog(basicEnemy);
            AssetDatabase.SaveAssets();

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            TruckController truckController = Object.FindFirstObjectByType<TruckController>();
            GameUIController gameUI = Object.FindFirstObjectByType<GameUIController>();
            if (gameManager == null || truckController == null || gameUI == null)
            {
                throw new InvalidOperationException("Enemy feature scene dependencies were not found.");
            }

            TruckDamageFlash damageFlash = GetOrAddComponent<TruckDamageFlash>(truckController.gameObject);
            TruckHealthController truckHealth = GetOrAddComponent<TruckHealthController>(truckController.gameObject);
            EnemyManager enemyManager = GetOrCreateSceneComponent<EnemyManager>("Enemy Manager");
            EnemySpawner enemySpawner = GetOrCreateSceneComponent<EnemySpawner>("Enemy Spawner");
            enemyManager.SetCatalog(catalog);

            SerializedObject serializedGameUI = new SerializedObject(gameUI);
            RectTransform leftPanel = (RectTransform)serializedGameUI.FindProperty("leftPanel").objectReferenceValue;
            RectTransform gameArea = (RectTransform)serializedGameUI.FindProperty("gameArea").objectReferenceValue;
            GameObject upgradePanel = (GameObject)serializedGameUI.FindProperty("upgradePanel").objectReferenceValue;
            if (leftPanel == null || gameArea == null)
            {
                throw new InvalidOperationException("Game UI panel references are missing.");
            }

            TruckHealthUIController healthUI = Object.FindFirstObjectByType<TruckHealthUIController>(FindObjectsInactive.Include);
            if (healthUI == null)
            {
                healthUI = CreateHealthUI(leftPanel);
            }
            else
            {
                healthUI.transform.SetParent(leftPanel, false);
            }

            Transform existingWarningUI = gameArea.Find("Enemy Warning UI") ?? gameArea.Find("Game Area UI/Enemy Warning UI");
            if (existingWarningUI != null)
            {
                Object.DestroyImmediate(existingWarningUI.gameObject);
            }

            EnemyWarningUIController warningUI = CreateWarningUI(gameArea);
            if (upgradePanel != null && upgradePanel.transform.parent == gameArea)
            {
                warningUI.transform.SetSiblingIndex(upgradePanel.transform.GetSiblingIndex());
            }

            gameManager.SetEnemySystems(truckHealth, damageFlash, healthUI, enemyManager, enemySpawner, warningUI);
            MainHudLayoutSetup.ApplyToLoadedScene();
            EditorUtility.SetDirty(enemyManager);
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "적 추적, 트럭 체력, HUD를 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            EnemyPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<EnemyPrefabCatalog>(EnemyCatalogPath);
            EnemyController prefab = AssetDatabase.LoadAssetAtPath<EnemyController>(BasicEnemyPrefabPath);
            if (config == null || catalog == null || prefab == null)
            {
                throw new InvalidOperationException("Enemy feature assets are missing.");
            }

            if (config.Truck.MaxHealth != 3 || !Mathf.Approximately(config.Truck.DamageInvulnerabilityDuration, 2f) ||
                config.Enemy.CountPerWantedLevel != 2 || config.Enemy.WantedSpeedBoostLevel != 5 ||
                !Mathf.Approximately(config.Enemy.WantedSpeedMultiplier, 2f))
            {
                throw new InvalidOperationException("Enemy feature configuration is incorrect.");
            }

            EnemyDefinition definition = prefab.GetComponent<EnemyDefinition>();
            if (definition == null || prefab.GetComponentInChildren<MeshRenderer>(true) == null || definition.ContactDamage != 1)
            {
                throw new InvalidOperationException("Basic enemy prefab is incomplete.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            TruckHealthController truckHealth = Object.FindFirstObjectByType<TruckHealthController>();
            TruckDamageFlash damageFlash = Object.FindFirstObjectByType<TruckDamageFlash>();
            TruckHealthUIController healthUI = Object.FindFirstObjectByType<TruckHealthUIController>();
            EnemyManager enemyManager = Object.FindFirstObjectByType<EnemyManager>();
            EnemySpawner enemySpawner = Object.FindFirstObjectByType<EnemySpawner>();
            EnemyWarningUIController warningUI = Object.FindFirstObjectByType<EnemyWarningUIController>(FindObjectsInactive.Include);
            if (gameManager == null || truckHealth == null || damageFlash == null || healthUI == null || enemyManager == null || enemySpawner == null || warningUI == null)
            {
                throw new InvalidOperationException("Enemy feature scene systems are missing.");
            }

            SerializedObject serializedHealthUI = new SerializedObject(healthUI);
            Text healthText = (Text)serializedHealthUI.FindProperty("healthText").objectReferenceValue;
            if (healthText == null || healthText.font != CartoonUIStyle.LoadFont())
            {
                throw new InvalidOperationException("Truck health text is not using the WebGL-safe HUD font.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            if (serializedGameManager.FindProperty("truckHealthController").objectReferenceValue != truckHealth
                || serializedGameManager.FindProperty("truckDamageFlash").objectReferenceValue != damageFlash
                || serializedGameManager.FindProperty("truckHealthUIController").objectReferenceValue != healthUI
                || serializedGameManager.FindProperty("enemyManager").objectReferenceValue != enemyManager
                || serializedGameManager.FindProperty("enemySpawner").objectReferenceValue != enemySpawner
                || serializedGameManager.FindProperty("enemyWarningUIController").objectReferenceValue != warningUI)
            {
                throw new InvalidOperationException("GameManager enemy references are incomplete.");
            }

            VerifyHealthAndInvulnerability(config);
            VerifyDeltaTimeMovement();
            VerifyWantedCountsAndRespawn(config, catalog);
            VerifyHealthSave(config);
            VerifyDefeatAndRespawn(config);
            VerifyOffscreenWarning(config, catalog);
            Debug.Log("Enemy feature verification passed.");
        }

        private static void VerifyHealthAndInvulnerability(GameConfig config)
        {
            GameObject truck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                TruckDamageFlash flash = truck.AddComponent<TruckDamageFlash>();
                TruckHealthController health = truck.AddComponent<TruckHealthController>();
                health.Initialize(config, flash);

                if (!health.TryTakeDamage(1) || health.CurrentHealth != 2 || !health.IsInvulnerable || !flash.IsFlashing)
                {
                    throw new InvalidOperationException("Truck did not enter invulnerability after taking damage.");
                }

                if (health.TryTakeDamage(1) || health.CurrentHealth != 2)
                {
                    throw new InvalidOperationException("Truck took damage during invulnerability.");
                }

                health.UpdateHealth(config.Truck.DamageInvulnerabilityDuration + 0.01f);
                if (health.IsInvulnerable || flash.IsFlashing || !truck.GetComponent<Renderer>().enabled)
                {
                    throw new InvalidOperationException("Truck damage flash did not end with invulnerability.");
                }
            }
            finally
            {
                Object.DestroyImmediate(truck);
            }
        }

        private static void VerifyDeltaTimeMovement()
        {
            GameObject truck = new GameObject("Enemy Movement Verification Truck");
            GameObject firstObject = new GameObject("Enemy Movement Verification A");
            GameObject secondObject = new GameObject("Enemy Movement Verification B");
            try
            {
                truck.transform.position = new Vector3(0f, 0f, 20f);
                EnemyData type = new EnemyData("verification", "verification", 1f, 0.5f, 6f, 1, 1f);
                EnemyController first = firstObject.AddComponent<EnemyDefinition>().gameObject.AddComponent<EnemyController>();
                EnemyController second = secondObject.AddComponent<EnemyDefinition>().gameObject.AddComponent<EnemyController>();
                first.Initialize(type, truck.transform);
                second.Initialize(type, truck.transform);

                first.UpdateEnemy(1f, false);
                second.UpdateEnemy(0.5f, false);
                second.UpdateEnemy(0.5f, false);
                if (Vector3.Distance(first.transform.position, second.transform.position) > 0.001f || !Mathf.Approximately(first.transform.position.z, 6f))
                {
                    throw new InvalidOperationException("Enemy movement changes with deltaTime step size.");
                }

                Vector3 pausedPosition = first.transform.position;
                first.UpdateEnemy(1f, true);
                if (first.transform.position != pausedPosition)
                {
                    throw new InvalidOperationException("Enemy moved while world time was stopped.");
                }
            }
            finally
            {
                Object.DestroyImmediate(secondObject);
                Object.DestroyImmediate(firstObject);
                Object.DestroyImmediate(truck);
            }
        }

        private static void VerifyWantedCountsAndRespawn(GameConfig config, EnemyPrefabCatalog catalog)
        {
            GameObject truck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject managerObject = new GameObject("Enemy Count Verification Manager");
            GameObject spawnerObject = new GameObject("Enemy Count Verification Spawner");
            GameObject wantedObject = new GameObject("Enemy Count Verification Wanted");
            try
            {
                TruckDamageFlash flash = truck.AddComponent<TruckDamageFlash>();
                TruckHealthController health = truck.AddComponent<TruckHealthController>();
                health.Initialize(config, flash);
                WantedLevelSystem wanted = wantedObject.AddComponent<WantedLevelSystem>();
                wanted.Initialize(config);
                EnemyManager manager = managerObject.AddComponent<EnemyManager>();
                manager.SetCatalog(catalog);
                manager.Initialize(config, truck.transform, health, wanted);
                EnemySpawner spawner = spawnerObject.AddComponent<EnemySpawner>();
                spawner.Initialize(config, manager, wanted, truck.transform);

                spawner.FillInitial();
                int levelZeroTarget = config.Enemy.MinimumCountForTesting;
                if (manager.Enemies.Count != levelZeroTarget)
                {
                    throw new InvalidOperationException(
                        $"Wanted level 0 enemy count mismatch. Expected {levelZeroTarget}, got {manager.Enemies.Count}.");
                }

                wanted.RestoreState(wanted.GetRequiredTotalKillsForLevel(1));
                spawner.FillInitial();
                int levelOneTarget = Mathf.Max(
                    config.Enemy.MinimumCountForTesting,
                    config.Enemy.CountPerWantedLevel);
                if (manager.Enemies.Count != levelOneTarget)
                {
                    throw new InvalidOperationException(
                        $"Wanted level 1 enemy count mismatch. Expected {levelOneTarget}, got {manager.Enemies.Count}.");
                }

                manager.Enemies[0].transform.position = new Vector3(config.Spawn.DespawnDistance + 1f, 0f, 0f);
                spawner.UpdateSpawner(config.Spawn.SpawnIntervalMilliseconds + 1f);
                if (manager.Enemies.Count != levelOneTarget)
                {
                    throw new InvalidOperationException("A despawned enemy was not replenished.");
                }

                wanted.RestoreState(wanted.GetRequiredTotalKillsForLevel(2));
                spawner.FillInitial();
                int levelTwoTarget = Mathf.Max(
                    config.Enemy.MinimumCountForTesting,
                    config.Enemy.CountPerWantedLevel * 2);
                if (manager.Enemies.Count != levelTwoTarget)
                {
                    throw new InvalidOperationException(
                        $"Wanted level 2 enemy count mismatch. Expected {levelTwoTarget}, got {manager.Enemies.Count}.");
                }

                EnemyController speedTestEnemy = manager.Enemies[0];
                float baseMoveSpeed = speedTestEnemy.Type.MoveSpeed;
                truck.transform.position = Vector3.zero;
                speedTestEnemy.transform.position = new Vector3(0f, speedTestEnemy.transform.position.y, 20f);
                wanted.RestoreState(wanted.GetRequiredTotalKillsForLevel(4));
                manager.UpdateEnemies(0.5f);
                if (!Mathf.Approximately(speedTestEnemy.transform.position.z, 20f - baseMoveSpeed * 0.5f))
                {
                    throw new InvalidOperationException("Police car speed increased before wanted level 5.");
                }

                speedTestEnemy.transform.position = new Vector3(0f, speedTestEnemy.transform.position.y, 20f);
                wanted.RestoreState(wanted.GetRequiredTotalKillsForLevel(5));
                manager.UpdateEnemies(0.5f);
                if (!Mathf.Approximately(speedTestEnemy.transform.position.z, 20f - baseMoveSpeed * config.Enemy.WantedSpeedMultiplier * 0.5f))
                {
                    throw new InvalidOperationException("Police car speed did not double at wanted level 5.");
                }

                manager.SetWorldPaused(true);
                Vector3 pausedPosition = manager.Enemies[0].transform.position;
                manager.UpdateEnemies(1f);
                if (manager.Enemies[0].transform.position != pausedPosition || health.CurrentHealth != health.MaxHealth)
                {
                    throw new InvalidOperationException("Enemy movement or contact continued during time stop.");
                }
            }
            finally
            {
                Object.DestroyImmediate(wantedObject);
                Object.DestroyImmediate(spawnerObject);
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(truck);
            }
        }

        private static void VerifyHealthSave(GameConfig config)
        {
            BlessingCatalog blessingCatalog = AssetDatabase.LoadAssetAtPath<BlessingCatalog>(BlessingCatalogPath);
            if (blessingCatalog == null)
            {
                throw new InvalidOperationException("Blessing catalog was not found for health save verification.");
            }

            PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            GameObject source = new GameObject("Enemy Health Save Source");
            GameObject restored = new GameObject("Enemy Health Save Restored");
            try
            {
                SaveSystems first = CreateSaveSystems(source, config, blessingCatalog);
                first.Health.TryTakeDamage(1);
                first.Save.Save();
                Object.DestroyImmediate(first.Save);

                SaveSystems second = CreateSaveSystems(restored, config, blessingCatalog);
                if (second.Health.CurrentHealth != 2 || second.Health.IsInvulnerable)
                {
                    throw new InvalidOperationException("Truck health was not restored correctly.");
                }
            }
            finally
            {
                Object.DestroyImmediate(restored);
                Object.DestroyImmediate(source);
                PlayerProgressSaveSystem.DeleteSaveForVerification(VerificationSaveKey);
            }
        }

        private static void VerifyDefeatAndRespawn(GameConfig config)
        {
            GameObject truckObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
            try
            {
                TruckController truck = truckObject.AddComponent<TruckController>();
                TruckDamageFlash flash = truckObject.AddComponent<TruckDamageFlash>();
                TruckHealthController health = truckObject.AddComponent<TruckHealthController>();
                PlayerState player = truckObject.AddComponent<PlayerState>();
                truck.Initialize(config);
                health.Initialize(config, flash);
                player.Initialize(config);
                player.RestoreState(5, 75, 120, 2, 0.5f, 0.4f);
                truck.UpgradeSpeed();
                truck.UpgradeSize();
                truck.transform.position = new Vector3(25f, truck.transform.position.y, -15f);
                truck.UpdateTruck(Vector2.up, 1f / 60f);

                Vector3 respawnPosition = Vector3.zero;
                health.Defeated += () =>
                {
                    player.ForfeitCurrentExperience();
                    truck.Respawn(respawnPosition, 0f);
                    health.Respawn();
                };

                if (!health.TryTakeDamage(config.Truck.MaxHealth))
                {
                    throw new InvalidOperationException("Fatal enemy damage was not applied.");
                }

                TruckController.TruckStats stats = truck.GetStats();
                if (player.Level != 5 || player.Exp != 0 || player.ExpRewardRemainder != 0f || player.Soul != 120 || player.UpgradePoints != 2)
                {
                    throw new InvalidOperationException("Respawn experience penalty changed unrelated player progress.");
                }

                if (health.CurrentHealth != health.MaxHealth || health.IsDefeated || !health.IsInvulnerable || !flash.IsFlashing)
                {
                    throw new InvalidOperationException("Truck health was not restored with respawn invulnerability.");
                }

                if (truck.transform.position.x != 0f || truck.transform.position.z != 0f || truck.CurrentSpeed != 0f || stats.SpeedLevel != 1 || stats.SizeLevel != 1)
                {
                    throw new InvalidOperationException("Truck respawn did not reset movement while preserving upgrades.");
                }
            }
            finally
            {
                Object.DestroyImmediate(truckObject);
            }
        }

        private static void VerifyOffscreenWarning(GameConfig config, EnemyPrefabCatalog catalog)
        {
            GameObject truck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            GameObject managerObject = new GameObject("Enemy Warning Verification Manager");
            GameObject cameraObject = new GameObject("Enemy Warning Verification Camera", typeof(UnityEngine.Camera));
            GameObject warningObject = new GameObject("Enemy Warning Verification UI", typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                TruckDamageFlash flash = truck.AddComponent<TruckDamageFlash>();
                TruckHealthController health = truck.AddComponent<TruckHealthController>();
                health.Initialize(config, flash);
                EnemyManager manager = managerObject.AddComponent<EnemyManager>();
                manager.SetCatalog(catalog);
                manager.Initialize(config, truck.transform, health);

                UnityEngine.Camera targetCamera = cameraObject.GetComponent<UnityEngine.Camera>();
                targetCamera.orthographic = true;
                targetCamera.orthographicSize = 5f;
                targetCamera.aspect = 1f;
                cameraObject.transform.position = new Vector3(0f, 10f, 0f);
                cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);

                RectTransform warningArea = warningObject.GetComponent<RectTransform>();
                warningArea.sizeDelta = new Vector2(800f, 1000f);
                CanvasGroup warningGroup = warningObject.GetComponent<CanvasGroup>();
                GameObject iconObject = new GameObject("Warning Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(EnemyWarningIconGraphic));
                iconObject.transform.SetParent(warningObject.transform, false);
                RectTransform warningIcon = iconObject.GetComponent<RectTransform>();
                warningIcon.sizeDelta = new Vector2(72f, 72f);
                EnemyWarningUIController warningUI = warningObject.AddComponent<EnemyWarningUIController>();
                warningUI.SetReferences(warningArea, warningGroup, warningIcon, 48f, 180f);
                warningUI.Initialize(config, manager, targetCamera, truck.transform);

                EnemyController enemy = manager.CreateEnemy("basic_enemy", new Vector3(0f, 0f, -6f));
                warningUI.UpdateWarning(0.01f);
                if (!warningUI.IsWarningVisible || warningUI.IconPosition.y >= 0f)
                {
                    throw new InvalidOperationException("Enemy warning did not appear at the lower edge.");
                }

                EnemyController leftEnemy = manager.CreateEnemy("basic_enemy", new Vector3(-6f, 0f, 0f));
                EnemyController rightEnemy = manager.CreateEnemy("basic_enemy", new Vector3(6f, 0f, 0f));
                warningUI.UpdateWarning(0.01f);
                if (warningUI.VisibleWarningCount != 3 || warningUI.GetIconPosition(0).y >= 0f || warningUI.GetIconPosition(1).x >= 0f || warningUI.GetIconPosition(2).x <= 0f)
                {
                    throw new InvalidOperationException("Warnings for enemies approaching from multiple directions were not displayed together.");
                }

                manager.Remove(leftEnemy);
                manager.Remove(rightEnemy);

                warningUI.UpdateWarning(config.Enemy.WarningBlinkInterval);
                if (Mathf.Approximately(warningGroup.alpha, 1f))
                {
                    throw new InvalidOperationException("Enemy warning did not blink.");
                }

                enemy.transform.position = new Vector3(0f, 0f, 6f);
                warningUI.UpdateWarning(0.01f);
                if (!warningUI.IsWarningVisible || warningUI.IconPosition.y <= 0f || warningUI.IconPosition.y > 320.01f)
                {
                    throw new InvalidOperationException("Enemy warning did not respect the wanted HUD space at the upper edge.");
                }

                enemy.transform.position = Vector3.zero;
                warningUI.UpdateWarning(0.01f);
                if (warningUI.IsWarningVisible)
                {
                    throw new InvalidOperationException("Enemy warning remained visible after the enemy entered the screen.");
                }

                enemy.transform.position = new Vector3(0f, 0f, -config.Enemy.OffscreenWarningDistance - 1f);
                warningUI.UpdateWarning(0.01f);
                if (warningUI.IsWarningVisible)
                {
                    throw new InvalidOperationException("Enemy warning appeared outside the configured warning distance.");
                }
            }
            finally
            {
                Object.DestroyImmediate(warningObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(truck);
            }
        }

        private static SaveSystems CreateSaveSystems(GameObject root, GameConfig config, BlessingCatalog catalog)
        {
            TruckController truck = root.AddComponent<TruckController>();
            TruckDamageFlash flash = root.AddComponent<TruckDamageFlash>();
            TruckHealthController health = root.AddComponent<TruckHealthController>();
            PlayerState player = root.AddComponent<PlayerState>();
            BlessingSystem blessings = root.AddComponent<BlessingSystem>();
            BlessingLoadoutSystem loadout = root.AddComponent<BlessingLoadoutSystem>();
            RebirthSystem rebirth = root.AddComponent<RebirthSystem>();
            WantedLevelSystem wanted = root.AddComponent<WantedLevelSystem>();
            TruckUpgradeSystem upgrades = root.AddComponent<TruckUpgradeSystem>();
            PlayerProgressSaveSystem save = root.AddComponent<PlayerProgressSaveSystem>();

            truck.Initialize(config);
            health.Initialize(config, flash);
            player.Initialize(config);
            blessings.SetCatalog(catalog);
            blessings.Initialize();
            loadout.Initialize(config, blessings);
            rebirth.Initialize(config, player, truck, blessings);
            wanted.Initialize(config);
            upgrades.Initialize(player, truck);
            save.SetSaveKeyForVerification(VerificationSaveKey);
            save.Initialize(player, truck, rebirth, blessings, loadout, wanted, health, upgrades);
            return new SaveSystems(health, save);
        }

        private static EnemyController GetOrCreateBasicEnemyPrefab()
        {
            Material material = GetOrCreateEnemyMaterial();
            EnemyController existing = AssetDatabase.LoadAssetAtPath<EnemyController>(BasicEnemyPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("BasicEnemy");
            try
            {
                EnemyDefinition definition = root.AddComponent<EnemyDefinition>();
                definition.Configure("basic_enemy", "경찰차", 2f, 0.5f, 6f, 1, 1f);
                root.AddComponent<EnemyController>();

                GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "VisualRoot";
                visual.transform.SetParent(root.transform, false);
                Object.DestroyImmediate(visual.GetComponent<BoxCollider>());
                visual.GetComponent<MeshRenderer>().sharedMaterial = material;

                GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, BasicEnemyPrefabPath);
                return prefab.GetComponent<EnemyController>();
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Material GetOrCreateEnemyMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(BasicEnemyMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
            {
                shader = Shader.Find("Standard");
            }

            material = new Material(shader)
            {
                name = "Basic Enemy Material",
                color = new Color32(0xd8, 0x38, 0x38, 0xff)
            };
            AssetDatabase.CreateAsset(material, BasicEnemyMaterialPath);
            return material;
        }

        private static EnemyPrefabCatalog GetOrCreateCatalog(EnemyController basicEnemy)
        {
            EnemyPrefabCatalog catalog = AssetDatabase.LoadAssetAtPath<EnemyPrefabCatalog>(EnemyCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<EnemyPrefabCatalog>();
                AssetDatabase.CreateAsset(catalog, EnemyCatalogPath);
            }

            catalog.SetPrefabs(new List<EnemyController> { basicEnemy });
            EditorUtility.SetDirty(catalog);
            return catalog;
        }

        private static TruckHealthUIController CreateHealthUI(Transform parent)
        {
            Font font = CartoonUIStyle.LoadFont();
            GameObject panel = new GameObject("Truck Health UI", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0f, 1f);
            panelRect.anchorMax = new Vector2(0f, 1f);
            panelRect.pivot = new Vector2(0f, 1f);
            panelRect.anchoredPosition = new Vector2(14f, -180f);
            panelRect.sizeDelta = new Vector2(200f, 48f);
            Image image = panel.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.55f);
            image.raycastTarget = false;

            GameObject textObject = new GameObject("Health Text", typeof(RectTransform), typeof(Text));
            textObject.transform.SetParent(panel.transform, false);
            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            Text text = textObject.GetComponent<Text>();
            text.font = font;
            text.text = "체력  ♥ ♥ ♥";
            text.fontSize = 23;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = Color.white;
            text.raycastTarget = false;
            UIFeedbackEffect feedback = textObject.AddComponent<UIFeedbackEffect>();
            feedback.Configure(0.18f, 0.06f);

            TruckHealthUIController controller = panel.AddComponent<TruckHealthUIController>();
            controller.SetReferences(text, feedback);
            return controller;
        }

        private static EnemyWarningUIController CreateWarningUI(Transform parent)
        {
            GameObject panel = new GameObject("Enemy Warning UI", typeof(RectTransform), typeof(CanvasGroup));
            panel.transform.SetParent(parent, false);
            RectTransform panelRect = panel.GetComponent<RectTransform>();
            panelRect.anchorMin = Vector2.zero;
            panelRect.anchorMax = Vector2.one;
            panelRect.offsetMin = Vector2.zero;
            panelRect.offsetMax = Vector2.zero;

            GameObject iconObject = new GameObject("Warning Icon", typeof(RectTransform), typeof(CanvasRenderer), typeof(EnemyWarningIconGraphic));
            iconObject.transform.SetParent(panel.transform, false);
            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = Vector2.zero;
            iconRect.sizeDelta = new Vector2(72f, 72f);
            EnemyWarningIconGraphic icon = iconObject.GetComponent<EnemyWarningIconGraphic>();
            icon.color = new Color(1f, 0.78f, 0.08f, 1f);
            icon.raycastTarget = false;

            EnemyWarningUIController controller = panel.AddComponent<EnemyWarningUIController>();
            controller.SetReferences(panelRect, panel.GetComponent<CanvasGroup>(), iconRect, 48f, 180f);
            panel.SetActive(false);
            return controller;
        }

        private static T GetOrCreateSceneComponent<T>(string objectName) where T : Component
        {
            T component = Object.FindFirstObjectByType<T>();
            if (component != null)
            {
                return component;
            }

            GameObject gameObject = new GameObject(objectName);
            return gameObject.AddComponent<T>();
        }

        private static T GetOrAddComponent<T>(GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            return component != null ? component : gameObject.AddComponent<T>();
        }

        private static void EnsureConfigDefaults(GameConfig config)
        {
            SerializedObject serializedConfig = new SerializedObject(config);
            SetIntIfZero(serializedConfig.FindProperty("truck.maxHealth"), 3);
            SetFloatIfZero(serializedConfig.FindProperty("truck.damageInvulnerabilityDuration"), 2f);
            SetFloatIfZero(serializedConfig.FindProperty("truck.damageFlashInterval"), 0.12f);
            SetIntIfZero(serializedConfig.FindProperty("enemy.countPerWantedLevel"), 2);
            SetFloatIfZero(serializedConfig.FindProperty("enemy.truckCollisionRadius"), 1.3f);
            SetFloatIfZero(serializedConfig.FindProperty("enemy.offscreenWarningDistance"), 25f);
            SetFloatIfZero(serializedConfig.FindProperty("enemy.warningBlinkInterval"), 0.35f);
            serializedConfig.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(config);
        }

        private static void SetIntIfZero(SerializedProperty property, int value)
        {
            if (property == null)
            {
                throw new InvalidOperationException("Enemy feature config property was not found.");
            }

            if (property.intValue <= 0)
            {
                property.intValue = value;
            }
        }

        private static void SetFloatIfZero(SerializedProperty property, float value)
        {
            if (property == null)
            {
                throw new InvalidOperationException("Enemy feature config property was not found.");
            }

            if (property.floatValue <= 0f)
            {
                property.floatValue = value;
            }
        }

        private static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path))
            {
                return;
            }

            int separatorIndex = path.LastIndexOf('/');
            string parent = path.Substring(0, separatorIndex);
            string folderName = path.Substring(separatorIndex + 1);
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, folderName);
        }

        private sealed class SaveSystems
        {
            public SaveSystems(TruckHealthController health, PlayerProgressSaveSystem save)
            {
                Health = health;
                Save = save;
            }

            public TruckHealthController Health { get; }
            public PlayerProgressSaveSystem Save { get; }
        }
    }
}
