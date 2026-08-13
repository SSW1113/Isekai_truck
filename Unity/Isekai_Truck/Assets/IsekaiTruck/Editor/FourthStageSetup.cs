using System;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Monsters;
using IsekaiTruck.Player;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class FourthStageSetup
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string MonsterDataPath = "Assets/IsekaiTruck/Data/monsters.json";
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";

        [MenuItem("Isekai Truck/Setup Collision Reward Stage")]
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

            GameObject playerObject = GameObject.Find("Player State");
            if (playerObject == null)
            {
                playerObject = new GameObject("Player State");
            }

            PlayerState playerState = playerObject.GetComponent<PlayerState>();
            if (playerState == null)
            {
                playerState = playerObject.AddComponent<PlayerState>();
            }

            gameManager.SetPlayerSystem(playerState);
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            ValidateSceneReferences();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "몬스터 충돌과 플레이어 보상 시스템을 Main 씬에 연결했습니다.",
                    "확인"
                );
            }
        }

        public static void Verify()
        {
            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(ConfigPath);
            TextAsset monsterDataFile = AssetDatabase.LoadAssetAtPath<TextAsset>(MonsterDataPath);

            if (config == null || monsterDataFile == null)
            {
                throw new InvalidOperationException("충돌 및 보상 검증용 데이터를 불러오지 못했습니다.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateSceneReferences();

            GameObject truckObject = new GameObject("Collision Verification Truck");
            GameObject managerObject = new GameObject("Collision Verification Manager");
            GameObject playerObject = new GameObject("Reward Verification Player");

            MonsterManager manager = managerObject.AddComponent<MonsterManager>();
            manager.SetDataFile(monsterDataFile);
            manager.Initialize(config, truckObject.transform);

            PlayerState playerState = playerObject.AddComponent<PlayerState>();
            playerState.Initialize(config);
            manager.MonsterDefeated += type => playerState.AddRewards(type.Exp, type.Soul);

            MonsterController escapingMonster = manager.CreateMonster("man", 1.77f, 0f);
            manager.UpdateMonsters();

            if (manager.Monsters.Count != 1)
            {
                throw new InvalidOperationException("AI 이동 후 충돌 검사 순서 검증에 실패했습니다.");
            }

            AssertApproximately(escapingMonster.transform.position.x, 1.81f, "충돌 전 도망 이동");
            manager.Remove(escapingMonster);

            truckObject.transform.localScale = Vector3.one * 2f;
            manager.CreateMonster("man", 3.5f, 0f);
            manager.UpdateMonsters();
            AssertPlayerState(playerState, 1, 50, 100, 2, 0, "첫 처치 보상");

            manager.CreateMonster("man", 3.5f, 0f);
            manager.UpdateMonsters();
            AssertPlayerState(playerState, 2, 0, 283, 4, 1, "레벨업 보상");

            GameObject multiLevelObject = new GameObject("Multi Level Verification Player");
            PlayerState multiLevelPlayer = multiLevelObject.AddComponent<PlayerState>();
            multiLevelPlayer.Initialize(config);
            RewardResult result = multiLevelPlayer.AddRewards(1000, 7);

            if (result.LevelUpCount != 3)
            {
                throw new InvalidOperationException($"다중 레벨업 횟수 검증 실패: expected 3, actual {result.LevelUpCount}");
            }

            AssertPlayerState(multiLevelPlayer, 4, 97, 800, 7, 3, "다중 레벨업");

            Object.DestroyImmediate(multiLevelObject);
            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(playerObject);
            Object.DestroyImmediate(truckObject);
            Debug.Log("Collision and reward stage verification passed.");
        }

        private static void ValidateSceneReferences()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();
            PlayerState playerState = Object.FindFirstObjectByType<PlayerState>();

            if (gameManager == null || monsterManager == null || playerState == null)
            {
                throw new InvalidOperationException("충돌 및 보상 시스템 씬 연결을 확인하지 못했습니다.");
            }

            SerializedObject gameManagerSerializedObject = new SerializedObject(gameManager);
            gameManagerSerializedObject.Update();

            if (gameManagerSerializedObject.FindProperty("monsterManager").objectReferenceValue == null ||
                gameManagerSerializedObject.FindProperty("playerState").objectReferenceValue == null)
            {
                throw new InvalidOperationException("GameManager의 충돌 또는 플레이어 참조가 비어 있습니다.");
            }
        }

        private static void AssertPlayerState(
            PlayerState playerState,
            int level,
            int exp,
            int requiredExp,
            int soul,
            int upgradePoints,
            string label
        )
        {
            PlayerSnapshot state = playerState.GetState();

            if (state.Level == level && state.Exp == exp && state.RequiredExp == requiredExp &&
                state.Soul == soul && state.UpgradePoints == upgradePoints)
            {
                return;
            }

            throw new InvalidOperationException(
                $"{label} 검증 실패: " +
                $"expected Lv.{level}, EXP {exp}/{requiredExp}, soul {soul}, points {upgradePoints}; " +
                $"actual Lv.{state.Level}, EXP {state.Exp}/{state.RequiredExp}, soul {state.Soul}, points {state.UpgradePoints}"
            );
        }

        private static void AssertApproximately(float actual, float expected, string label)
        {
            if (Mathf.Abs(actual - expected) <= 0.0001f)
            {
                return;
            }

            throw new InvalidOperationException($"{label} 검증 실패: expected {expected}, actual {actual}");
        }
    }
}
