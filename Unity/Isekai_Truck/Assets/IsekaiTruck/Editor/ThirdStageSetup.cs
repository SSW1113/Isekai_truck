using System;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Monsters;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace IsekaiTruck.Editor
{
    public static class ThirdStageSetup
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string MonsterDataPath = "Assets/IsekaiTruck/Data/monsters.json";
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";

        [MenuItem("Isekai Truck/Setup Monster AI Stage")]
        public static void Setup()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Isekai Truck", "플레이 모드를 종료한 뒤 실행해주세요.", "확인");
                return;
            }

            TextAsset monsterDataFile = AssetDatabase.LoadAssetAtPath<TextAsset>(MonsterDataPath);
            if (monsterDataFile == null)
            {
                throw new InvalidOperationException($"몬스터 데이터를 불러오지 못했습니다: {MonsterDataPath}");
            }

            Scene scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();

            if (gameManager == null)
            {
                throw new InvalidOperationException("Main 씬에서 GameManager를 찾지 못했습니다.");
            }

            GameObject monsterRoot = GameObject.Find("Monsters");
            if (monsterRoot == null)
            {
                monsterRoot = new GameObject("Monsters");
            }

            MonsterManager monsterManager = monsterRoot.GetComponent<MonsterManager>();
            if (monsterManager == null)
            {
                monsterManager = monsterRoot.AddComponent<MonsterManager>();
            }

            monsterManager.SetDataFile(monsterDataFile);
            gameManager.SetMonsterSystem(monsterManager);
            EditorUtility.SetDirty(monsterManager);
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            ValidateSceneReferences();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "몬스터 데이터와 AI 시스템을 Main 씬에 연결했습니다.",
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
                throw new InvalidOperationException("몬스터 AI 검증용 데이터를 불러오지 못했습니다.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateSceneReferences();

            GameObject truckObject = new GameObject("Monster AI Verification Truck");
            GameObject managerObject = new GameObject("Monster AI Verification Manager");
            MonsterManager manager = managerObject.AddComponent<MonsterManager>();
            manager.SetDataFile(monsterDataFile);
            manager.Initialize(config, truckObject.transform);
            float referenceDeltaTime = 1f / config.ReferenceFrameRate;

            if (manager.Types.Count != 3 || manager.Types["man"].Name != "평범한 사람")
            {
                throw new InvalidOperationException("monsters.json 타입 또는 이름 검증에 실패했습니다.");
            }

            AssertApproximately(
                manager.Types["policeman"].Speed,
                MonsterDefinition.DefaultSpeed,
                "백수 속도");
            AssertApproximately(
                manager.Types["salesman"].SpawnWeight,
                MonsterDefinition.DefaultSpawnWeight,
                "영업사원 스폰 가중치");

            MonsterData manType = manager.Types["man"];
            MonsterController fleeingMonster = manager.CreateMonster("man", 5f, 0f);
            AssertApproximately(fleeingMonster.transform.position.y, manType.Size, "몬스터 높이");
            AssertApproximately(
                fleeingMonster.transform.localScale.x,
                manType.Size * 2f,
                "몬스터 지름");

            manager.UpdateMonsters(referenceDeltaTime);
            AssertApproximately(
                fleeingMonster.transform.position.x,
                5f + manType.Speed,
                "몬스터 도망 속도");

            fleeingMonster.transform.position = new Vector3(-2f, manType.Size, 0f);
            manager.UpdateMonsters(referenceDeltaTime);
            AssertApproximately(
                fleeingMonster.transform.position.x,
                -2f + manType.Speed,
                "근거리 도망 방향 고정");

            MonsterController wanderingMonster = manager.CreateMonster("man", 100f, 0f);
            Vector3 wanderStart = wanderingMonster.transform.position;
            manager.UpdateMonsters(referenceDeltaTime);
            float wanderDistance = Vector3.Distance(wanderStart, wanderingMonster.transform.position);
            AssertApproximately(wanderDistance, manType.Speed * 0.2f, "몬스터 배회 속도");

            Object.DestroyImmediate(managerObject);
            Object.DestroyImmediate(truckObject);
            Debug.Log("Monster AI stage verification passed.");
        }

        private static void ValidateSceneReferences()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            MonsterManager monsterManager = Object.FindFirstObjectByType<MonsterManager>();

            if (gameManager == null || monsterManager == null)
            {
                throw new InvalidOperationException("몬스터 AI 시스템 씬 연결을 확인하지 못했습니다.");
            }

            SerializedObject gameManagerSerializedObject = new SerializedObject(gameManager);
            gameManagerSerializedObject.Update();

            if (gameManagerSerializedObject.FindProperty("monsterManager").objectReferenceValue == null)
            {
                throw new InvalidOperationException("GameManager의 MonsterManager 참조가 비어 있습니다.");
            }

            SerializedObject monsterManagerSerializedObject = new SerializedObject(monsterManager);
            monsterManagerSerializedObject.Update();

            if (monsterManagerSerializedObject.FindProperty("monsterDataFile").objectReferenceValue == null)
            {
                throw new InvalidOperationException("MonsterManager의 monsters.json 참조가 비어 있습니다.");
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
    }
}
