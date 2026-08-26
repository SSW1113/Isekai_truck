using System;
using IsekaiTruck.Config;
using IsekaiTruck.Core;
using IsekaiTruck.Monsters;
using IsekaiTruck.Spawn;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

namespace IsekaiTruck.Editor
{
    public static class FifthStageSetup
    {
        private const string ConfigPath = "Assets/IsekaiTruck/Config/GameConfig.asset";
        private const string MonsterDataPath = "Assets/IsekaiTruck/Data/monsters.json";
        private const string ScenePath = "Assets/IsekaiTruck/Scenes/Main.unity";

        [MenuItem("Isekai Truck/Setup Monster Spawn Stage")]
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

            GameObject spawnerObject = GameObject.Find("Monster Spawner");
            if (spawnerObject == null)
            {
                spawnerObject = new GameObject("Monster Spawner");
            }

            MonsterSpawner monsterSpawner = spawnerObject.GetComponent<MonsterSpawner>();
            if (monsterSpawner == null)
            {
                monsterSpawner = spawnerObject.AddComponent<MonsterSpawner>();
            }

            gameManager.SetSpawnSystem(monsterSpawner);
            EditorUtility.SetDirty(gameManager);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();

            ValidateSceneReferences();

            if (!Application.isBatchMode)
            {
                EditorUtility.DisplayDialog(
                    "Isekai Truck",
                    "몬스터 스폰 시스템을 Main 씬에 연결했습니다.",
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
                throw new InvalidOperationException("스폰 검증용 데이터를 불러오지 못했습니다.");
            }

            EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            ValidateSceneReferences();

            GameObject truckObject = new GameObject("Spawn Verification Truck");
            GameObject managerObject = new GameObject("Spawn Verification Manager");
            GameObject spawnerObject = new GameObject("Spawn Verification Spawner");
            Random.State randomState = Random.state;

            try
            {
                MonsterManager manager = managerObject.AddComponent<MonsterManager>();
                manager.SetDataFile(monsterDataFile);
                manager.Initialize(config, truckObject.transform);

                MonsterSpawner spawner = spawnerObject.AddComponent<MonsterSpawner>();
                spawner.Initialize(config, manager, truckObject.transform);

                Random.InitState(20260813);
                spawner.FillInitial();
                ValidateInitialMonsters(config, manager, truckObject.transform);

                MonsterController boundaryMonster = manager.Monsters[0];
                boundaryMonster.transform.position = truckObject.transform.position + Vector3.right * config.Spawn.DespawnDistance;
                spawner.UpdateSpawner(0f);

                if (manager.Monsters.Count != config.Spawn.TargetCount)
                {
                    throw new InvalidOperationException("제거 거리 경계의 몬스터가 잘못 제거됐습니다.");
                }

                boundaryMonster.transform.position += Vector3.right * 0.01f;
                spawner.UpdateSpawner(0f);
                AssertCount(manager, config.Spawn.TargetCount - 1, "먼 몬스터 제거");

                spawner.UpdateSpawner(config.Spawn.SpawnIntervalMilliseconds - 0.001f);
                AssertCount(manager, config.Spawn.TargetCount - 1, "스폰 간격 이전");

                spawner.UpdateSpawner(config.Spawn.SpawnIntervalMilliseconds);
                AssertCount(manager, config.Spawn.TargetCount, "스폰 간격 도달");

                MonsterController spawnedMonster = manager.Monsters[manager.Monsters.Count - 1];
                AssertSpawnDistance(config, spawnedMonster.transform.position, truckObject.transform.position);
            }
            finally
            {
                Random.state = randomState;
                Object.DestroyImmediate(spawnerObject);
                Object.DestroyImmediate(managerObject);
                Object.DestroyImmediate(truckObject);
            }

            Debug.Log("Monster spawn stage verification passed.");
        }

        private static void ValidateInitialMonsters(GameConfig config, MonsterManager manager, Transform truck)
        {
            AssertCount(manager, config.Spawn.TargetCount, "초기 몬스터 수");
            AssertSpawnWeight(manager, "man");
            AssertSpawnWeight(manager, "salesman");
            AssertSpawnWeight(manager, "policeman");

            int manCount = 0;
            int salesmanCount = 0;
            int policemanCount = 0;

            for (int i = 0; i < manager.Monsters.Count; i++)
            {
                MonsterController monster = manager.Monsters[i];
                AssertSpawnDistance(config, monster.transform.position, truck.position);

                switch (monster.Type.Id)
                {
                    case "man":
                        manCount++;
                        break;
                    case "salesman":
                        salesmanCount++;
                        break;
                    case "policeman":
                        policemanCount++;
                        break;
                    default:
                        throw new InvalidOperationException($"알 수 없는 몬스터 타입이 스폰됐습니다: {monster.Type.Id}");
                }
            }

            if (manCount == 0 || salesmanCount == 0 || policemanCount == 0)
            {
                throw new InvalidOperationException(
                    $"동일 spawnWeight 스폰 검증 실패: man {manCount}, salesman {salesmanCount}, policeman {policemanCount}"
                );
            }
        }

        private static void AssertSpawnWeight(MonsterManager manager, string typeId)
        {
            if (!manager.Types.TryGetValue(typeId, out MonsterData type))
            {
                throw new InvalidOperationException($"몬스터 타입을 찾지 못했습니다: {typeId}");
            }

            if (!Mathf.Approximately(type.SpawnWeight, MonsterDefinition.DefaultSpawnWeight))
            {
                throw new InvalidOperationException(
                    $"spawnWeight 검증 실패: {typeId}, expected {MonsterDefinition.DefaultSpawnWeight}, actual {type.SpawnWeight}"
                );
            }
        }

        private static void AssertSpawnDistance(GameConfig config, Vector3 monsterPosition, Vector3 truckPosition)
        {
            float dx = monsterPosition.x - truckPosition.x;
            float dz = monsterPosition.z - truckPosition.z;
            float distance = Mathf.Sqrt(dx * dx + dz * dz);

            if (distance < config.Spawn.MinDistance - 0.001f || distance > config.Spawn.MaxDistance + 0.001f)
            {
                throw new InvalidOperationException($"스폰 거리 검증 실패: {distance}");
            }
        }

        private static void AssertCount(MonsterManager manager, int expected, string label)
        {
            if (manager.Monsters.Count == expected)
            {
                return;
            }

            throw new InvalidOperationException($"{label} 검증 실패: expected {expected}, actual {manager.Monsters.Count}");
        }

        private static void ValidateSceneReferences()
        {
            GameManager gameManager = Object.FindFirstObjectByType<GameManager>();
            MonsterSpawner monsterSpawner = Object.FindFirstObjectByType<MonsterSpawner>();

            if (gameManager == null || monsterSpawner == null)
            {
                throw new InvalidOperationException("몬스터 스폰 시스템 씬 연결을 확인하지 못했습니다.");
            }

            SerializedObject gameManagerSerializedObject = new SerializedObject(gameManager);
            gameManagerSerializedObject.Update();

            if (gameManagerSerializedObject.FindProperty("monsterSpawner").objectReferenceValue == null)
            {
                throw new InvalidOperationException("GameManager의 MonsterSpawner 참조가 비어 있습니다.");
            }
        }
    }
}
