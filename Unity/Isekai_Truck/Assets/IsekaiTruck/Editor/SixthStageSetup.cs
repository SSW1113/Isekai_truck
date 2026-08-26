using System;
using IsekaiTruck.Camera;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Monsters;
using IsekaiTruck.Player;
using IsekaiTruck.Truck;
using IsekaiTruck.Upgrades;
using IsekaiTruck.World;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class SixthStageSetup
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string MonsterDataPath = "Assets/IsekaiTruck/Data/monsters.json";
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";

        [MenuItem("Isekai Truck/Setup Truck Upgrade Stage")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이 모드를 종료한 뒤 실행해주세요.", "확인");
                return;
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            if (gameManager == null)
            {
                throw new InvalidOperationException("Main 씬에서 GameManager를 찾지 못했습니다.");
            }

            GameObject upgradeObject = GameObject.Find("Truck Upgrade System");
            if (upgradeObject == null)
            {
                upgradeObject = new GameObject("Truck Upgrade System");
            }

            TruckUpgradeSystem upgradeSystem = upgradeObject.GetComponent<TruckUpgradeSystem>();
            if (upgradeSystem == null)
            {
                upgradeSystem = upgradeObject.AddComponent<TruckUpgradeSystem>();
            }

            gameManager.SetUpgradeSystem(upgradeSystem);
            EditorUtility.SetDirty(gameManager);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            Verify();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "트럭 업그레이드 시스템을 Main 씬에 연결했습니다.", "확인");
            }
        }

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            TextAsset monsterDataFile = AssetDatabase.LoadAssetAtPath<TextAsset>(MonsterDataPath);
            if (config == null || monsterDataFile == null)
            {
                throw new InvalidOperationException("업그레이드 검증용 설정을 불러오지 못했습니다.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateSceneReference();

            GameObject truckObject = new GameObject("Upgrade Verification Truck");
            GameObject playerObject = new GameObject("Upgrade Verification Player");
            GameObject upgradeObject = new GameObject("Upgrade Verification System");
            GameObject cameraObject = new GameObject("Upgrade Verification Camera");
            GameObject worldObject = new GameObject("Upgrade Verification World");
            GameObject monsterManagerObject = new GameObject("Upgrade Verification Monsters");

            try
            {
                truckObject.transform.position = new Vector3(0f, 0.5f, 0f);
                TruckController truck = truckObject.AddComponent<TruckController>();
                truck.Initialize(config);

                PlayerState player = playerObject.AddComponent<PlayerState>();
                player.Initialize(config);

                TruckUpgradeSystem upgrades = upgradeObject.AddComponent<TruckUpgradeSystem>();
                upgrades.Initialize(player, truck);

                if (upgrades.TryUpgradeSpeed())
                {
                    throw new InvalidOperationException("포인트가 없는데 속도 업그레이드가 적용됐습니다.");
                }

                player.AddRewards(player.RequiredExp);
                if (!upgrades.TryUpgradeSpeed())
                {
                    throw new InvalidOperationException("속도 업그레이드가 적용되지 않았습니다.");
                }

                AssertApproximately(
                    truck.GetStats().MaxSpeed,
                    config.Truck.BaseMaxSpeed + config.Truck.SpeedPerUpgrade,
                    "속도 업그레이드"
                );
                AssertPoints(player, 0, "속도 업그레이드 포인트 소비");

                AddLevelAndUpgradeSize(player, upgrades);
                AssertApproximately(truck.transform.localScale.x, 1f + config.Truck.SizePerUpgrade, "크기 업그레이드");
                AssertApproximately(truck.transform.position.y, 0.5f * truck.transform.localScale.x, "크기 업그레이드 높이");

                AddLevelAndUpgradeSize(player, upgrades);
                AddLevelAndUpgradeSize(player, upgrades);

                UnityEngine.Camera targetCamera = cameraObject.AddComponent<UnityEngine.Camera>();
                CameraController cameraController = cameraObject.AddComponent<CameraController>();
                cameraController.Initialize(config, truckObject.transform);

                float zoomMultiplier = 1f;
                for (int i = 0; i < 120; i++)
                {
                    zoomMultiplier = cameraController.UpdateCamera(1f / config.ReferenceFrameRate);
                }

                if (zoomMultiplier <= 1f)
                {
                    throw new InvalidOperationException("트럭 크기 증가가 카메라 줌에 반영되지 않았습니다.");
                }

                WorldManager worldManager = worldObject.AddComponent<WorldManager>();
                worldManager.Initialize(config, truckObject.transform, targetCamera);
                worldManager.UpdateWorld(zoomMultiplier);

                if (RenderSettings.fogEndDistance <= config.World.BaseFogFar)
                {
                    throw new InvalidOperationException("카메라 줌 증가가 Fog에 반영되지 않았습니다.");
                }

                MonsterManager monsterManager = monsterManagerObject.AddComponent<MonsterManager>();
                monsterManager.SetDataFile(monsterDataFile);
                monsterManager.Initialize(config, truckObject.transform);
                MonsterData manType = monsterManager.Types["man"];
                float collisionDistance = config.Monster.CollisionDistance * truckObject.transform.localScale.x;
                monsterManager.CreateMonster("man", collisionDistance - manType.Speed - 0.05f, 0f);
                monsterManager.UpdateMonsters(1f / config.ReferenceFrameRate);

                if (monsterManager.Monsters.Count != 0)
                {
                    throw new InvalidOperationException("트럭 실제 크기가 몬스터 충돌 판정에 반영되지 않았습니다.");
                }
            }
            finally
            {
                Object.DestroyImmediate(monsterManagerObject);
                Object.DestroyImmediate(worldObject);
                Object.DestroyImmediate(cameraObject);
                Object.DestroyImmediate(upgradeObject);
                Object.DestroyImmediate(playerObject);
                Object.DestroyImmediate(truckObject);
            }

            Debug.Log("Truck upgrade stage verification passed.");
        }

        private static void AddLevelAndUpgradeSize(PlayerState player, TruckUpgradeSystem upgrades)
        {
            player.AddRewards(player.RequiredExp);
            if (!upgrades.TryUpgradeSize())
            {
                throw new InvalidOperationException("크기 업그레이드가 적용되지 않았습니다.");
            }

            AssertPoints(player, 0, "크기 업그레이드 포인트 소비");
        }

        private static void AssertPoints(PlayerState player, int expected, string label)
        {
            if (player.UpgradePoints != expected)
            {
                throw new InvalidOperationException($"{label} 검증 실패: expected {expected}, actual {player.UpgradePoints}");
            }
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Mathf.Abs(actual - expected) <= 0.0001f)
            {
                return;
            }

            throw new InvalidOperationException($"{label} 검증 실패: expected {expected}, actual {actual}");
        }

        private static void ValidateSceneReference()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            TruckUpgradeSystem upgradeSystem = Object.FindFirstObjectByType<TruckUpgradeSystem>();
            if (gameManager == null || upgradeSystem == null)
            {
                throw new InvalidOperationException("트럭 업그레이드 시스템 씬 연결을 확인하지 못했습니다.");
            }

            SerializedObject serializedGameManager = new SerializedObject(gameManager);
            serializedGameManager.Update();

            if (serializedGameManager.FindProperty("truckUpgradeSystem").objectReferenceValue != upgradeSystem)
            {
                throw new InvalidOperationException("GameManager의 TruckUpgradeSystem 참조가 비어 있습니다.");
            }
        }
    }
}
